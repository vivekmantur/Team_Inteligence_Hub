# Team Intelligence Hub — Contributions Schema

**Version:** 1.1
**Owner:** Nihar Pulluri
**Audience:** Engineering
**Status:** Proposal. Not built. Agree the shape before writing entities and the migration.
**Derived from:** `src/components/contribution/AddContributionWizard.tsx`, `src/components/contribution/ContributionContext.tsx`
**Published diagram:** https://claude.ai/code/artifact/e7e1ee07-5bfb-4e75-946a-7f2952e3c4f0

### Change log

| Version | Change |
| --- | --- |
| 1.1 | Twelve tables reduced to eight. `ContributionTags`, `ContributionTypeSelections`, and `ContributionReuseTargets` became JSON columns on the root row using EF Core 9 primitive collections. `ContributionSupportingAssets` folded into `ContributionLinks`. Section 4 has the verification. |
| 1.0 | First draft, twelve tables. |

---

## 1. Summary

Eight tables derived field by field from the eight-step Add Contribution wizard.

| Count | Item |
| --- | --- |
| 8 | new tables |
| 11 | foreign keys |
| 3 | JSON columns on the root row |
| 1 | unique index |
| 6 | new enums |
| 0 | columns added to existing tables |

The wizard collects one flat object on the client (the `Contribution` interface in `ContributionContext.tsx`). Four of the eight steps now write into a single row.

---

## 2. Where each wizard step lands

| Step | Screen | Lands in | Shape |
| --- | --- | --- | --- |
| 1 | Initiative | `Contributions.InitiativeId` | FK |
| 2 | Type | `Contributions.Types` | JSON array |
| 3 | Summary | `Contributions.Title`, `.Description`, `.KeyTakeaway`, `.Priority`, `.Tags` | columns + JSON array |
| 4 | Details | `ContributionMetrics` / `Risks` / `AiPractices` / `CustomerStories` | 0:1 table each |
| 5 | Evidence | `ContributionAttachments`, `ContributionLinks` | 1:n each |
| 6 | Contributors | `ContributionContributors` | 1:n |
| 7 | Visibility | `Contributions.ReuseTargets` | JSON array |
| 8 | Review | `Contributions.Status` | column |

Step 4 still shapes the design. Its sections appear only when the matching type is selected in step 2, so each stays an optional 1:1 table rather than a block of nullable columns on the root row. A Progress Update physically cannot carry a risk severity.

---

## 3. What collapsed, and what resisted

The dividing line is whether a row carries anything of its own beyond the value.

### Collapsed into the root row

| Was a table | Became |
| --- | --- |
| `ContributionTypeSelections` | `Contributions.Types nvarchar(200)` |
| `ContributionTags` | `Contributions.Tags nvarchar(1000)` |
| `ContributionReuseTargets` | `Contributions.ReuseTargets nvarchar(400)` |
| `ContributionSupportingAssets` | folded into `ContributionLinks` (adds a `Description` column) |

The first three were tables whose only columns were an id, a parent id, and one value. That is exactly what EF Core 9 primitive collections are for. The fourth collected a link and a description, which `ContributionLinks` already stores.

### Resisted, and why

- **`ContributionContributors`** carries `ResponsibilityArea` and `IsPrimary` per row, and a foreign key to `Users`. JSON cannot express a foreign key, and "contributions by person" drives the Team Contributions page.
- **`ContributionAttachments`** needs a row id for the download and delete endpoints, and a blob lifecycle to match.
- **`ContributionLinks`** holds objects, not primitives, so primitive collections do not apply.
- **The four detail tables** hold the analytic payload: filtering risks by severity, summing time saved, computing metric deltas, pulling quotes for Testimonials. Buried in a blob, every one of those becomes an `OPENJSON` exercise. `ContributionRisks` also has a foreign key to `Users`.

---

## 4. Evidence the JSON columns work

Verified against EF Core 9.0.0 on net9.0, the versions in `TeamIntelligenceHub.Infrastructure.csproj`, in a throwaway project rather than from memory.

### Model mapping: one table, no shadow entity

```
Id     clr=Int32          column=int              isPrimitiveCollection=False
Title  clr=String         column=nvarchar(200)    isPrimitiveCollection=False
Tags   clr=List<string>   column=nvarchar(1000)   isPrimitiveCollection=True
Types  clr=List<enum>     column=nvarchar(200)    isPrimitiveCollection=True
tables in model = 1
```

The enum stores member names, not integers, so the column stays readable in SSMS.

### The queries still translate to SQL

None of these fall back to client evaluation.

Filter by one tag, from `c => c.Tags.Contains("APAC")`:

```sql
WHERE N'APAC' IN (
    SELECT [t].[value]
    FROM OPENJSON([c].[Tags]) WITH ([value] nvarchar(max) '$') AS [t]
)
```

Tag vocabulary for the typeahead, from `SelectMany(c => c.Tags).Distinct()`:

```sql
SELECT DISTINCT [t].[value]
FROM [Contributions] AS [c]
CROSS APPLY OPENJSON([c].[Tags]) WITH ([value] nvarchar(max) '$') AS [t]
```

Tag facet counts, from `SelectMany().GroupBy().Count()`:

```sql
SELECT [t].[value] AS [Tag], COUNT(*) AS [Count]
FROM [Contributions] AS [c]
CROSS APPLY OPENJSON([c].[Tags]) WITH ([value] nvarchar(max) '$') AS [t]
GROUP BY [t].[value]
```

### Edit works by mutating the list

The concern with a JSON column is whether EF notices a tag added or removed in place, or only on reassignment. It notices.

```
after attach          : Unchanged
after Tags.Add(...)   : Modified   TagsModified=True
after Tags.Remove(...): Modified   value=APAC,Pilot
value comparer        : ListOfReferenceTypesComparer
deep-compares lists   : True
```

So edit is `contribution.Tags.Remove("FY26")` then `SaveChangesAsync()`. No delete-and-reinsert, which is what `ActivityRepository.ReplaceMentionsAsync` has to do today for a child table.

### Configuration

```csharp
builder.PrimitiveCollection(c => c.Tags).HasMaxLength(1000);

builder.PrimitiveCollection(c => c.Types)
    .HasMaxLength(200)
    .ElementType(e => e.HasConversion<string>());

builder.PrimitiveCollection(c => c.ReuseTargets)
    .HasMaxLength(400)
    .ElementType(e => e.HasConversion<string>());
```

Set `HasMaxLength` on all three. Without it EF emits `nvarchar(max)`, which is stored off-row and reads slower.

---

## 5. Delete behaviour

SQL Server permits only one cascade path into any table. The app already spends that budget on `Initiatives`, so `Users` gets the restricting edges.

```
Initiatives ──CASCADE──> Contributions ──CASCADE (x7)──> child tables

Users ──RESTRICT──> Contributions.SubmittedByUserId
Users ──RESTRICT──> ContributionContributors.UserId
Users ──RESTRICT──> ContributionRisks.OwnerUserId
```

Deleting an Initiative removes its Contributions and everything hanging off them, in one path. Tags, types, and reuse targets go with the row because they are columns on it. Deleting a User is refused while any contribution names them as submitter, contributor, or risk owner, which matches how `InitiativeMembers` and `Tasks` already behave.

The blob objects behind `ContributionAttachments` are not removed by the database. Deleting those is the service's job, exactly as with task comment attachments.

---

## 6. Root table

### `Contributions`

One row per contribution, draft or submitted. Fourteen columns, three of which carry a whole list each.

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | int identity | no | PK |
| `InitiativeId` | int | no | FK `Initiatives.Id`, cascade. Step 1. |
| `SubmittedByUserId` | int | no | FK `Users.Id`, restrict. Taken from the token, never from the request body. |
| `Title` | nvarchar(200) | no | Step 3, required by the wizard's Next gate. |
| `Description` | nvarchar(4000) | no | Step 3, also required by the gate. |
| `KeyTakeaway` | nvarchar(500) | yes | One quotable sentence. |
| `Priority` | nvarchar(20) | no | enum `ContributionPriority`, defaults to Medium. |
| `Status` | nvarchar(20) | no | enum `ContributionStatus`, defaults to Draft. Step 8 sets it. |
| `Types` | nvarchar(200) | no | JSON array of `ContributionType` names. Step 2. Dedupe in the service. |
| `Tags` | nvarchar(1000) | no | JSON array of free text. Step 3. Trim and dedupe in the service. |
| `ReuseTargets` | nvarchar(400) | no | JSON array of `ContributionReuseTarget` names. Step 7. |
| `SubmittedAt` | datetime2 | yes | Set once, when Status moves to Submitted. Null on drafts. |
| `CreatedAt` | datetime2 | no | UTC, via the existing value converter. |
| `UpdatedAt` | datetime2 | yes | |

Suggested indexes: `(InitiativeId, CreatedAt DESC)` for the feed, `(SubmittedByUserId)` for "my contributions".

**Not stored:** the client type carries `initiativeName` and `workstream` alongside `initiativeId`. Both belong to the Initiative and should be projected into the response DTO, the way `ActivityResponseDto` already fills `UserDisplayName`. Copying them onto the row lets them drift the moment an Initiative is renamed.

---

## 7. People and evidence

Three tables that survive because each row carries more than a value.

### `ContributionContributors`

Step 6. The submitter is row one and cannot be removed in the UI.

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | int identity | no | PK |
| `ContributionId` | int | no | FK cascade. UQ with `UserId`. |
| `UserId` | int | no | FK `Users.Id`, restrict. UQ. |
| `ResponsibilityArea` | nvarchar(255) | yes | Same length as `InitiativeMember.ResponsibilityArea`. |
| `IsPrimary` | bit | no | The Primary / Supporting toggle. Several rows may be primary. |
| `AddedAt` | datetime2 | no | |

### `ContributionAttachments`

Metadata only. The bytes live in blob storage. Mirrors `TaskCommentAttachment` column for column, so the existing `IFileStorage` abstraction and download endpoint pattern carry straight over.

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | int identity | no | PK |
| `ContributionId` | int | no | FK cascade |
| `FileName` | nvarchar(255) | no | What the person called it. |
| `BlobName` | nvarchar(500) | no | Key in the container. Prefix `contributions/{id}`. |
| `ContentType` | nvarchar(100) | no | |
| `FileSize` | bigint | no | Bytes. 25 MB ceiling, same constant as task attachments. |
| `CreatedAt` | datetime2 | no | |

### `ContributionLinks`

Step 4 assets and step 5 links both land here.

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | int identity | no | PK |
| `ContributionId` | int | no | FK cascade |
| `Source` | nvarchar(20) | no | enum `ContributionLinkSource` |
| `Url` | nvarchar(2048) | no | Validate the scheme is http or https before storing. |
| `Label` | nvarchar(200) | yes | The wizard falls back to the URL when blank. Do that in the UI, not the column. |
| `Description` | nvarchar(2000) | yes | Absorbed from the dropped SupportingAssets table. |
| `CreatedAt` | datetime2 | no | |

---

## 8. Conditional detail tables

Each uses `ContributionId` as both primary key and foreign key, a shared primary key. That gives a real 1:1 for free: the row cannot exist without its contribution, and there cannot be two of them. A contribution tagged with three types gets three of these rows, one per table.

### `ContributionMetrics` — type: Business Metric

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `ContributionId` | int | no | PK and FK cascade |
| `MetricName` | nvarchar(150) | no | |
| `Unit` | nvarchar(50) | yes | users, %, hours |
| `PreviousValue` | decimal(18,4) | yes | Numeric. See decision 3. |
| `CurrentValue` | decimal(18,4) | yes | Numeric. See decision 3. |
| `ReportingPeriod` | nvarchar(50) | yes | Free text. The wizard defaults it to "Q3 FY26". |

### `ContributionRisks` — type: Risk / Blocker

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `ContributionId` | int | no | PK and FK cascade |
| `Description` | nvarchar(2000) | no | |
| `Severity` | nvarchar(20) | no | enum `RiskSeverity`, defaults to Medium |
| `BusinessImpact` | nvarchar(2000) | yes | |
| `Mitigation` | nvarchar(2000) | yes | |
| `SupportNeeded` | nvarchar(500) | yes | |
| `OwnerUserId` | int | yes | FK `Users.Id`, restrict. See decision 4. |
| `TargetResolutionDate` | date | yes | See decision 5. |

### `ContributionAiPractices` — type: AI Best Practice

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `ContributionId` | int | no | PK and FK cascade |
| `Tool` | nvarchar(100) | no | Free text. The wizard defaults it to "Copilot". |
| `UseCase` | nvarchar(2000) | yes | |
| `Prompt` | nvarchar(max) | yes | Pasted verbatim so others can reuse it. No length ceiling worth guessing. |
| `TimeSavedHoursPerWeek` | decimal(9,2) | yes | See decision 6. |
| `Recommendation` | nvarchar(2000) | yes | |

### `ContributionCustomerStories` — type: Customer Story

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `ContributionId` | int | no | PK and FK cascade |
| `CustomerName` | nvarchar(200) | no | |
| `Summary` | nvarchar(4000) | yes | |
| `Outcome` | nvarchar(300) | yes | "43% faster onboarding" |
| `Quote` | nvarchar(2000) | yes | What the Testimonials page will read. |
| `BusinessValue` | nvarchar(500) | yes | |

---

## 9. New enums

Stored as strings with `HasConversion<string>()`, matching the five that already exist. Three of the six are element types of a JSON collection rather than scalar columns, which the probe confirmed stores member names, not integers.

| Enum | Values | Used as |
| --- | --- | --- |
| `ContributionType` | ProgressUpdate, Deliverable, CustomerStory, BusinessMetric, Risk, Decision, AiBestPractice, Testimonial, SupportingAsset, SupportNeeded, Other | element type of `Contributions.Types`. Closed set: the wizard has an explicit "Other" card. |
| `ContributionReuseTarget` | ExecutiveBrief, LeadershipUpdate, Qbr, CustomerStoryLibrary, KnowledgeRepository, BestPractices, TeamNewsletter, VivaEngage | element type of `Contributions.ReuseTargets`, matching `REUSE_TARGETS`. |
| `ContributionLinkSource` | SharePoint, Teams, Loop, OneDrive, ExternalUrl | scalar on `ContributionLinks`, matching `LINK_SOURCES`. |
| `ContributionPriority` | Low, Medium, High, Critical | scalar. Deliberately not `InitiativePriority`, which has no Critical. |
| `ContributionStatus` | Draft, Submitted | scalar. Room to add Archived later. |
| `RiskSeverity` | Low, Medium, High, Critical | scalar. Same members as `ContributionPriority` but a different concept, kept apart the way the codebase already keeps `InitiativePriority` and `InitiativeTaskPriority` apart. |

---

## 10. Decisions to make before the migration

Places where the wizard's current shape and a schema that will still be useful in a year disagree. Numbers 3 through 7 all involve changing a text input in the frontend.

1. **The submitter is hardcoded.** The wizard uses a `SUBMITTER` constant, "Nihar Pulluri". Backend takes the author from the token via `ICurrentUserService`, the way `ActivityService` and `TaskCommentService` do, and the wizard reads the real person from `useBackendUser`.

2. **The initiative and people lists are mock data.** Steps 1 and 6 read from `@/data/mock`. They need `useInitiatives` and the shared `UserPicker`, which already exist and are already wired to the API.

3. **Metric values are strings.** `previousValue` and `currentValue` are text inputs. Stored as `decimal(18,4)`, Analytics can compute the delta and chart it. Stored as text, it can only be displayed. The wizard needs numeric inputs.

4. **The risk owner is a name, not a person.** The field defaults to the submitter's display name and accepts anything. A `UserPicker` and an `OwnerUserId` make "risks I own" a query instead of a string match. Left nullable so an unassigned risk is still recordable.

5. **Target resolution is free text.** The placeholder suggests "End of Q3". A `date` makes overdue risks findable. If fuzzy periods genuinely matter more than sorting, it stays `nvarchar(100)`.

6. **Time saved is free text.** "4 hrs / week" cannot be summed, and summing it across contributions is the obvious reason to capture it. A number plus a fixed "hours per week" label in the UI is the smaller change.

7. **File size is a formatted string.** The client stores "1.4 MB". Store `bigint` bytes and format on display, which is what `humanSize()` in the wizard already does and what `TaskCommentAttachment.FileSize` already holds.

8. **The JSON columns give up two things.** A child table enforced `UNIQUE(ContributionId, Tag)`. A JSON array does not, so nothing stops `["APAC","APAC"]`. Dedupe in the service, the way `ActivityService.ResolveMentionsAsync` already does for mentions. Tag filters also cannot use an index: `OPENJSON` means a scan plus a parse per row. At a few thousand contributions that is milliseconds. Past roughly a hundred thousand it would need a persisted computed column, or promotion back to a table. That promotion is a straightforward migration, so this is not a one-way door.

9. **Tags will fragment without a typeahead.** Copilot, copilot, and co-pilot become three tags for one idea. Add `GET /api/contributions/tags?q=` over the distinct query in section 4, so the second person to type "cop" is offered what the first person created. The Initiative create form already does this against a hardcoded array at `initiatives-new.tsx:88`.

10. **Should adding a contributor enrol them on the Initiative?** Assigning a task already adds the assignee to `InitiativeMembers` automatically. The same question applies here, and the answer should match so the Team tab stays trustworthy.

11. **Uploads are simulated.** Step 5's progress bar is a `setInterval` counting to 100. Real uploads hit the same ordering constraint that came up on task comments: the row must exist before its attachments. Saving a draft first gives a `ContributionId` to upload against, which the wizard's Save draft button already fits.

12. **The Decision type has no detail section.** Its card promises "record a key decision and rationale" but step 4 has nothing for it. Either add a fifth conditional section and table, or accept that Description carries it. This is a UI gap, not a schema one, so it does not block the migration.

---

## 11. Build order

Same three steps as the previous tables. One migration for the whole set.

**Step 1 — Backend entities and migration.**
Six enums in `TeamIntelligenceHub.Domain/Enums`, eight entity classes with their `const` length limits, eight EF configurations including the three `PrimitiveCollection` calls, one migration. Verify with `has-pending-model-changes`, then update the database.

**Step 2 — API.**
`ContributionsController` at `/api/initiatives/{id}/contributions` for the list and create, plus `/api/contributions/{id}` for read, update, and delete. Attachments follow the task pattern: `/api/contributions/{id}/attachments` to upload, and a download route. One create call carries the whole graph, since the wizard submits all eight steps at once.

**Step 3 — Frontend connection.**
Replace the mock arrays in the wizard, add `src/hooks/use-contributions.ts`, and point the Team Contributions page and the Initiative detail Contributions count at the API instead of `ContributionContext` state.

---

## 12. Prerequisite

Confirm the `AddActivity` migration is applied on the database. It was still pending at the last check. Running `Update-Database` will apply both if it is not.
