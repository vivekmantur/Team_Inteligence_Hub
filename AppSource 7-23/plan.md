---
appName: Team Intelligence Hub
appDescription: A strategic execution, change management, transformation, and organizational knowledge platform where every effort is captured as an Initiative and every team is captured as an Initiative Team.
isPlanMode: false
---

# Team Intelligence Hub

## Latest: Initiative Team (central collaboration model)

The Initiative model has been enhanced from a single owner into a full **Initiative Team**. Every Initiative now carries members, roles, responsibility areas, allocation, tasks, comments, activity, and notifications — and integrates with Contributions, Knowledge Repository, Analytics, and AI Copilot.

### Data model additions
- `InitiativeTeamMember` — name, role, `roleOther`, responsibility area, optional allocation %, avatar, added-at.
- Roles: **Owner, Contributor, Change Manager, Communications Lead, Analytics Lead, Other (specify)**.
- `InitiativeTask` — title, description, assignee, due date, priority (High/Medium/Low), status (Not Started / In Progress / Blocked / Done), from-mention flag.
- `CommentEntry` — author, text, extracted mentions.
- `ActivityEntry` — initiative-created, member-added, member-removed, task-created, task-status, comment, contribution, mention.
- `NotificationEntry` — initiative-assignment, task-assignment, mention, comment, contribution. Every notification carries `channels: ["email", "in-app"]`.

### Notifications (email + in-app)
- Bell in the top bar with unread badge, dropdown, click-through to the Initiative, and mark-all-read.
- Assigning a member sends `initiative-assignment` notifications to every added member.
- Creating a task with an assignee sends a `task-assignment` notification.
- Posting a comment with `@Name` sends a `mention` notification.

### Initiative Detail page (`/initiatives/:id`)
Tabs: **Overview**, **Team**, **Tasks**, **Activity**.

- **Overview** — progress bar, deliverables, owner card, Copilot summary grounded on team/tasks/contributions, analytics link.
- **Team tab** — multi-user people picker to add members with role, responsibility area, and allocation. Each row shows role, area, allocation, and per-member counts for **contributions**, **tasks (open/total)**, and **activity**. Owner is protected from removal.
- **Tasks tab** — lightweight task manager (create with assignee, due date, priority; filter by status; click circle to toggle done; inline status change). Auto-notifies assignees.
- **Activity tab** — unified feed with a composer that supports **@mentions** (typeahead against team members). Toggle to auto-create tasks from @mentions. Comments render with highlighted mention chips.

### Initiative creation flow
When an Initiative is created:
- Owner is automatically seeded as the first team member (role = Owner, 100% allocation).
- Every other assigned member is notified via email + in-app.
- Activity feed logs the creation event.

### Cross-surface integration
- **Contributions** — per-initiative contribution counts flow into member stats on the Team tab.
- **Knowledge Repository** — assets uploaded via Add Contribution stay linked to their Initiative.
- **Analytics** — Overview tab links out to `/analytics` for aggregate-by-Initiative views.
- **AI Copilot** — Copilot summary on the Overview and Activity tabs is grounded on team, tasks, and contributions; “Ask Copilot about this Initiative” shortcut jumps to `/copilot`.

### Initiatives list enhancements
Initiative cards now surface **Team size**, **Open tasks**, and **Deliverables**, and route to the new Detail page via **Open** or clicking the card.

## Previously shipped
- **Terminology** — Projects renamed to Initiatives across nav, routes, pages, empty states, forms, Contributions, Knowledge, and Copilot. Legacy `/projects` routes redirect.
- **New Initiative page** (`/initiatives/new`) — compact MVP form (Details, Ownership, Timeline, Outcomes) with sticky footer, live summary panel, Save as Draft, Create Initiative, success screen.
- **Add Contribution** — single reusable multi-step wizard launched from Home, Initiatives list, Team Contributions, and Knowledge Repository.
