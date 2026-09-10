# Feature: Backend-Powered Content Studio Generation

## Goal

Allow users to generate Content Studio output from backend-owned Initiative and Contribution data, using Azure OpenAI. The frontend sends only the user's selections; the backend loads the structured data, optionally retrieves Initiative-scoped document context for narrative formats, builds the prompt, and returns the generated content.

## In scope

- Replace frontend mock generation with a typed backend generation request.
- Support LinkedIn Post, Viva Engage Post, Newsletter, Executive Summary, QBR Slide, Blog, and Case Study.
- Send the selected Initiative, format, tone, audience, and length from the frontend.
- Load Contribution data in the backend for the selected Initiative.
- Load related data from ContributionMetrics, ContributionCustomerStories, ContributionRisks, and ContributionAiPractices as applicable per format.
- Use structured Initiative and Contribution data for all formats.
- Use Initiative-scoped document retrieval only for Blog and Case Study.
- Send the approved context to Azure OpenAI through the backend.
- Display loading, success, error, retry, and copy states in Content Studio.

## Out of scope

- Redesigning the Content Studio page.
- Allowing users to select individual Contributions, metrics, risks, stories, or attachments.
- Company-wide document retrieval for Content Studio generation.
- Document retrieval for LinkedIn Post, Viva Engage Post, Newsletter, Executive Summary, or QBR Slide.
- Loading `ContributionLinks` or `ContributionContributors` — no in-scope format uses either. If a future format needs attribution (for example, crediting contributors in a Newsletter), add it then with its own business rule.
- Introducing Initiative-level ownership/membership authorization. This endpoint follows the same authorization model every existing Initiative-scoped read uses today: the Initiative must exist, and no new per-Initiative ownership or membership check is introduced by this feature.
- Automatically publishing generated content.
- Approval workflows for generated content.
- Streaming responses unless an existing backend contract is reused and explicitly approved.
- Persisting generated content unless explicitly approved.
- Changing the Contribution upload workflow.

## User flow

1. The user opens Content Studio.
2. The user selects a content format, tone, audience, and length.
3. The user selects an Initiative.
4. The user selects Generate with Copilot.
5. The frontend validates the required selections.
6. The frontend sends only the Initiative ID and generation selections to the backend.
7. The backend confirms that the Initiative exists.
8. The backend loads the structured Initiative and Contribution context from the database.
9. If the format is Blog or Case Study, the backend retrieves relevant document context from attachments belonging to that Initiative only.
10. The backend constructs the format-specific prompt and calls Azure OpenAI.
11. The backend returns the generated content and limited response metadata.
12. The frontend displays the generated content.
13. The user can copy the result or regenerate using the current selections.
14. A failure displays a retryable error without exposing prompts, secrets, or unauthorized data.

## Business rules

1. The frontend sends only `initiativeId`, `format`, `tone`, `audience`, and `length`.
2. The frontend does not send Contribution, Metric, Risk, Customer Story, AI Practice, or Attachment records.
3. The backend is responsible for loading all structured context from the database.
4. All loaded records must belong to the selected Initiative through the existing relationship model.
5. The backend must not trust client-provided child-record IDs.
6. Authorization matches the existing Initiative-read model used by `InitiativesController.GetById` and `ContributionService.RequireInitiativeAsync` today: any authenticated user, gated only on the Initiative existing. No new per-Initiative ownership or membership check is introduced by this feature. If tighter access control is wanted later, it should be proposed as its own change applied consistently across Initiative reads.
7. LinkedIn Post uses structured data including ContributionMetric, ContributionCustomerStory.Quote, and Contribution.KeyTakeaway.
8. Viva Engage Post uses structured data, with emphasis on AiBestPractice and team wins.
9. Newsletter uses structured data across several Contributions, prioritizing Title and KeyTakeaway across Deliverable, ProgressUpdate, Metric, and CustomerStory contributions.
10. Executive Summary uses structured ContributionMetric, ContributionRisk, and Contribution.KeyTakeaway data.
11. QBR Slide uses structured ContributionMetric data for Metrics and Initiative.ExpectedOutcome, Initiative.SuccessMeasures, and Initiative.KeyObjective for Roadmap content.
12. Blog uses ContributionMetric and ContributionCustomerStory data, plus the base Contribution fields (Title, Description, KeyTakeaway) from Contributions typed Deliverable or ProgressUpdate. Those types have no dedicated detail table, so their narrative content is the Contribution Description field, not a separate child record. Blog also uses Initiative-scoped document retrieval.
13. Case Study uses ContributionCustomerStory.Summary, Outcome, Quote, and BusinessValue as the backbone plus Initiative-scoped document retrieval.
14. Document retrieval is enabled only for Blog and Case Study. ContributionAttachments are not loaded at all for any other format.
15. Retrieval must be restricted to authorized attachments belonging to the selected Initiative.
16. Records and document text are untrusted input and must not override backend system instructions.
17. Azure OpenAI credentials, deployment configuration, and system prompts remain backend-only.
18. A generation request must not be submitted again while an identical request is in progress from the same page.

## Data / state changes

### Frontend request

```json
{
  "initiativeId": 123,
  "format": "Executive Summary",
  "tone": "Confident",
  "audience": "Leadership",
  "length": "Medium"
}
```

The frontend tracks:

- Selected format, tone, audience, length, and Initiative.
- Idle, loading, success, and error generation states.
- Generated content and retry behavior.

### Backend structured context

The backend loads data for the selected Initiative from the existing schema, scoped to what each format actually uses:

- Contributions
- ContributionMetrics
- ContributionCustomerStories
- ContributionRisks
- ContributionAiPractices
- ContributionAttachments — Blog and Case Study only, for document retrieval
- Initiative.ExpectedOutcome
- Initiative.SuccessMeasures
- Initiative.KeyObjective
- Initiative fields needed to identify the Initiative and its owner/context

At minimum, metric context preserves:

- MetricName
- PreviousValue
- CurrentValue
- Unit

A calculated change may be included only when numeric values make the calculation valid.

### Backend response

Proposed response shape:

```json
{
  "content": "Generated content...",
  "format": "Executive Summary",
  "initiativeId": 123,
  "usedDocumentRetrieval": false,
  "sourceCount": 0,
  "generationId": "optional-id"
}
```

The exact response must follow existing backend DTO conventions and must not expose unauthorized source content or internal prompts.

## Interfaces / integrations

### Frontend

Use the existing API client and hook conventions. The Content Studio page should call a typed backend function instead of the current frontend `craftContent` mock path.

Likely frontend areas:

- `AppSource 7-23/src/pages/content-studio.tsx`
- `AppSource 7-23/src/lib/api-client.ts`
- An existing or new generation hook under `AppSource 7-23/src/hooks/`

### Backend

Reuse existing controller, DTO, validator, application service, authorization, attachment, retrieval, and Azure OpenAI patterns where available.

The endpoint shape is provisional and must follow existing backend conventions. A possible shape is:

```http
POST /api/initiatives/{initiativeId}/content-generation
```

Which controller owns this route is an open question: it may belong under InitiativesController by URL convention, or alongside CopilotController because both use the chat-completion client. The backend must validate the request, confirm the Initiative exists, load structured data, conditionally retrieve documents, construct the format-specific prompt, call Azure OpenAI, and return the result.

### Format context matrix

| Format | Structured context | Document retrieval |
|---|---|---|
| LinkedIn Post | ContributionMetric, ContributionCustomerStory.Quote, Contribution.KeyTakeaway | No |
| Viva Engage Post | Structured Contribution data, especially AiBestPractice and team wins | No |
| Newsletter | Several Contributions: Title and KeyTakeaway across Deliverable, ProgressUpdate, Metric, CustomerStory | No |
| Executive Summary | ContributionMetric, ContributionRisk, Contribution.KeyTakeaway | No |
| QBR Slide | ContributionMetric plus Initiative.ExpectedOutcome, SuccessMeasures, KeyObjective | No |
| Blog | ContributionMetric, ContributionCustomerStory, plus Title/Description/KeyTakeaway from Deliverable- and ProgressUpdate-typed Contributions | Yes, selected Initiative only |
| Case Study | ContributionCustomerStory.Summary, Outcome, Quote, BusinessValue | Yes, selected Initiative only |

## Edge and failure cases

- No Initiative selected.
- Initiative does not exist.
- Initiative existence check fails.
- Initiative has no Contributions.
- A required child table has no rows.
- Blog or Case Study has no eligible attachments.
- Unsupported format, tone, audience, or length.
- Attachment retrieval timeout or failure.
- Azure OpenAI timeout, rate limit, or service failure.
- Empty or malformed Azure OpenAI response.
- Network interruption from the frontend.
- Repeated Generate clicks while a request is running.
- User changes selections while generation is running.
- Very large structured data or retrieved document context.
- Retrieved content attempts prompt injection.
- Copy is clicked before generated content exists.
- A failed request must not be presented as a successful generation.
- Retrieval must never return documents from another Initiative.

## Security / privacy

- Require the same authentication mechanism used by existing backend APIs.
- Confirm the selected Initiative exists before loading structured data or attachments, using the same check existing Initiative-scoped reads use today.
- Do not introduce a new per-Initiative ownership or membership gate for this endpoint.
- Never trust client-provided child-record IDs or document IDs.
- Keep Azure OpenAI credentials and deployment settings on the backend.
- Do not log full Contribution data, document text, secrets, or generated content unnecessarily.
- Treat database content and retrieved document text as untrusted prompt input.
- Do not expose unauthorized Initiative existence or document details through errors.
- Apply existing sensitivity, redaction, and approval rules if present.

## Non-functional requirements

### Performance

- Do not perform document retrieval for short, social, digest, summary, or numbers-driven formats.
- Scope Blog and Case Study retrieval to the selected Initiative.
- Define limits for child rows, retrieved documents, excerpt size, and total prompt size.
- Prevent duplicate in-flight requests from the same page.
- Show the loading state immediately after submission.

### Accessibility

- The Generate button exposes disabled and loading states.
- Errors are visible to screen readers.
- Generated output is keyboard accessible and selectable.
- Existing controls retain visible focus states.
- Generation status changes use an appropriate live region where needed.

### Observability

- Record generation start, success, and failure without logging sensitive content.
- Include Initiative ID, format, retrieval-enabled status, duration, and failure category.
- Distinguish structured-data, retrieval, and Azure OpenAI failures.
- Include a correlation or generation ID when supported by existing conventions.

## Acceptance criteria

- [ ] Given valid selections, when the user generates content, then the frontend sends only Initiative ID, format, tone, audience, and length.
- [ ] Given LinkedIn Post, when generation runs, then the backend uses structured Contribution data and does not perform document retrieval.
- [ ] Given Viva Engage Post, when generation runs, then the backend emphasizes structured AiBestPractice and team-win data and does not perform document retrieval.
- [ ] Given Newsletter, when generation runs, then the backend uses structured data from several Contributions and does not perform document retrieval.
- [ ] Given Executive Summary, when generation runs, then the backend uses ContributionMetric, ContributionRisk, and KeyTakeaway data and does not perform document retrieval.
- [ ] Given QBR Slide, when generation runs, then the backend uses ContributionMetric and the selected Initiative roadmap fields and does not perform document retrieval.
- [ ] Given Blog, when generation runs, then the backend uses the specified structured fields and retrieves only documents attached to the selected Initiative.
- [ ] Given Case Study, when generation runs, then the backend uses the specified Customer Story fields and retrieves only documents attached to the selected Initiative.
- [ ] Given an Initiative that does not exist, when generation is requested, then the backend rejects it with a 404 before loading context or retrieving documents.
- [ ] Given a missing required selection, when the user tries to generate, then the frontend prevents submission and explains what is missing.
- [ ] Given invalid request values, when the backend receives them, then it returns a documented validation error.
- [ ] Given retrieval fails for Blog or Case Study, when generation is requested, then the approved retrieval-failure policy is applied and unrelated documents are never used.
- [ ] Given generation is in progress, when the user clicks Generate again, then a duplicate in-flight request is not submitted.
- [ ] Given Azure OpenAI succeeds, when the response returns, then the frontend displays the generated content.
- [ ] Given Azure OpenAI or retrieval fails, when the error returns, then the frontend shows a retryable error without exposing secrets, prompts, or unauthorized data.
- [ ] Given generated content exists, when the user clicks Copy, then the content is copied to the clipboard.
- [ ] Automated tests cover validation, existence checks, format routing, structured context mapping, retrieval enablement, Initiative scoping, provider failure, and duplicate submission prevention.

## Open questions / explicit assumptions

- Assumption: QBR Slide does not use document retrieval.
- Assumption: Newsletter does not use document retrieval.
- Assumption: Executive Summary does not use document retrieval.
- Assumption: Viva Engage Post does not use document retrieval.
- Assumption: The backend loads all Contributions for the selected Initiative rather than receiving Contribution IDs from the client.
- Assumption: This feature introduces no Initiative-level ownership or membership authorization beyond the existing Initiative-scoped read model. Any tighter access control should be a separate cross-cutting change.
- Open question: Which controller owns the endpoint — InitiativesController or alongside CopilotController?
- Open question: How will per-type Contribution filtering be queried? If Types is a JSON PrimitiveCollection without a reliable translated query precedent, should the backend load the bounded Initiative list and filter in memory?
- Open question: Should generated content be persisted in the backend?
- Open question: Should generation return a complete response or stream tokens?
- Open question: What exact attachment types are eligible for Blog and Case Study retrieval?
- Open question: What happens when Blog or Case Study has no eligible attachment documents?
- Open question: Should retrieval failure block generation or allow structured-data-only fallback?
- Open question: What maximum number of documents and total context size are allowed?
- Open question: What Azure OpenAI deployment should be used?
- Open question: Which existing Copilot retrieval mechanism should be reused?
