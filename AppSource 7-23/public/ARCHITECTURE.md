# Team Intelligence Hub — Application Architecture

**Version:** 1.0  
**Owner:** Nihar Pulluri  
**Audience:** Engineering, Architecture Review, Change & Adoption Leadership  
**Status:** Design document for production build-out

---

## 1. Executive Summary

The **Team Intelligence Hub** is an enterprise-grade, AI-powered platform for Microsoft Change Management and Adoption teams. It acts as the central intelligence layer that captures team projects, deliverables, customer stories, testimonials, feedback, and analytics — and turns them into executive-ready outputs such as Quarterly Business Reviews (QBRs), LinkedIn posts, case studies, and executive briefs.

The application is designed around three pillars:

1. **Capture** — Structured intake of projects, people, evidence, and analytics.
2. **Reason** — AI-powered retrieval and generation grounded in approved evidence.
3. **Produce** — Template-driven outputs (QBR decks, social posts, exec summaries) with mandatory human review.

The frontend is a modern React + TypeScript SPA using Fluent 2 / Copilot design language. The recommended backend is a Microsoft-first stack: **Entra ID, Dataverse, SharePoint, Azure Blob, Azure Functions, Azure OpenAI, Azure AI Search, and Azure AI Document Intelligence**, orchestrated by Azure API Management and Event Grid.

---

## 2. Goals and Non-Goals

### Goals
- Provide a single system of record for team projects, contributions, and evidence.
- Enable frequent, low-friction uploads from team members tied to specific projects.
- Ground all AI outputs in approved, cited evidence.
- Generate QBR decks, social posts, and executive content from the same evidence base.
- Enforce governance: approval status, sensitivity labels, external-safe gating.
- Deliver a Copilot-class experience (streaming answers, citations, quick actions).

### Non-Goals
- Replace Power BI / Fabric as the enterprise BI layer (the app consumes curated data, it does not warehouse telemetry).
- Replace Microsoft 365 for collaboration (files remain in SharePoint; the app orchestrates and reasons).
- Auto-publish content to external channels (all outputs require human approval).

---

## 3. High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        Frontend (React SPA)                      │
│  Home · Analytics · Projects · Team · Stories · Testimonials     │
│  Role Hub · Agents · Content Studio · Repository · Copilot       │
└──────────────────────────┬───────────────────────────────────────┘
                           │  HTTPS + Entra ID token
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Azure API Management (APIM)                   │
│         Auth · Rate limiting · Routing · Observability           │
└──────────────────────────┬───────────────────────────────────────┘
                           │
     ┌─────────────────────┼──────────────────────┐
     ▼                     ▼                      ▼
┌──────────┐        ┌──────────────┐       ┌──────────────┐
│ Projects │        │  Copilot RAG │       │  Content     │
│  API     │        │  API         │       │  Studio API  │
│ (Funcs)  │        │  (Funcs)     │       │  (Funcs)     │
└────┬─────┘        └──────┬───────┘       └──────┬───────┘
     │                     │                      │
     ▼                     ▼                      ▼
┌──────────────────────────────────────────────────────────────────┐
│                          Data Plane                              │
│  Dataverse · SharePoint · Azure Blob · Azure AI Search           │
└──────────────────────────────────────────────────────────────────┘
                           ▲
                           │  Event Grid
┌──────────────────────────┴───────────────────────────────────────┐
│                  File Processing Pipeline (Functions)            │
│  Extract → Normalize → Chunk → Embed → Enrich → Index            │
│  (Document Intelligence · Video Indexer · Azure OpenAI)          │
└──────────────────────────────────────────────────────────────────┘
```

---

## 4. Architectural Layers

The backend is designed as **five clean layers**, each with a single responsibility.

| Layer | Responsibility | Primary Technology |
|---|---|---|
| **1. Ingestion** | Accept uploads, form submissions, connector data | Azure Functions, Power Automate, SharePoint, Microsoft Graph |
| **2. Storage** | Hold raw files, structured records, vectors | SharePoint, Dataverse, Azure Blob, Azure AI Search |
| **3. Processing** | Extract, normalize, enrich, chunk, embed | Azure Functions, Document Intelligence, Azure OpenAI Embeddings |
| **4. Intelligence** | Reasoning, RAG, generation, summarization | Azure OpenAI (GPT-4o), Azure AI Search, Copilot Studio |
| **5. API / Orchestration** | Serve frontend, enforce auth, coordinate flows | Azure API Management + Azure Functions / Container Apps |

### Design Principles

- **Stateless APIs, stateful data plane.** All compute is horizontally scalable; state lives in the data plane.
- **Auth once at the edge.** Entra ID at APIM; downstream services trust validated tokens.
- **Separate raw, curated, and approved.** Evidence flows through explicit approval gates.
- **Event-driven processing.** File ingestion, embedding, and indexing are asynchronous.
- **Business logic in code, not prompts.** KPIs are calculated with versioned formulas before being handed to the model.
- **Cite everything.** Every AI-generated statement carries a citation to an approved source.

---

## 5. Frontend Architecture

### Stack
- **Framework:** React 18 + TypeScript
- **Build:** Vite + Bun
- **Styling:** Tailwind CSS + Shadcn UI
- **Routing:** `react-router-dom`
- **State / Data:** `@tanstack/react-query`
- **Icons:** `lucide-react`
- **Charts:** `d3`
- **Design language:** Microsoft Fluent 2 / Copilot inspired

### Navigation

A persistent left navigation rail with the following destinations:

| Route | Purpose |
|---|---|
| `/` Home | KPI overview, quick actions, AI assistant entry point |
| `/analytics` | Adoption, engagement, sentiment dashboards |
| `/projects` | Project catalog and detail views |
| `/team` | Team contributions and accomplishment summaries |
| `/stories` | Customer Zero repository |
| `/testimonials` | Testimonial capture and theme summary |
| `/role-hub` | Role Hub analytics |
| `/agents` | Agent analytics |
| `/content-studio` | AI content generation (LinkedIn, QBR, exec briefs, etc.) |
| `/repository` | Knowledge repository (all uploaded assets) |
| `/copilot` | Ask Copilot chat surface |
| `/admin` | Configuration, users, approvals |

### Frontend Responsibilities
- Render UI shell immediately; use skeleton shimmers for async sections.
- Load data progressively via React Query.
- Stream Copilot and Content Studio responses via Server-Sent Events (SSE).
- Enforce optimistic UI where safe; reconcile from server on completion.
- Never hold business logic — all KPI math happens server-side.

---

## 6. Data Model

The operational data model lives in **Dataverse**. Large binaries live in **SharePoint** or **Azure Blob**. Vectors and text chunks live in **Azure AI Search**.

### Core Tables

| Table | Purpose | Key Fields |
|---|---|---|
| `ReportingPeriod` | Fiscal quarter / reporting window | `period_id`, `name`, `start_date`, `end_date`, `status` |
| `Project` | Unit of team work | `project_id`, `name`, `owner_id`, `workstream`, `status`, `period_id` |
| `ProjectMember` | People allocated to a project | `project_id`, `person_id`, `role`, `allocation_pct`, `start_date`, `end_date` |
| `TeamMember` | Person record | `person_id`, `name`, `email`, `manager_id`, `capacity_hours_per_week` |
| `Deliverable` | Output of a project | `deliverable_id`, `project_id`, `name`, `type`, `status`, `owner_id` |
| `ContentAsset` | Uploaded file (metadata) | `asset_id`, `project_id`, `sharepoint_url`, `type`, `sensitivity`, `approval_status` |
| `Metric` / `CalculatedKPI` | Approved KPI values | `kpi_id`, `project_id`, `period_id`, `kpi_name`, `value`, `formula_version`, `confidence` |
| `CustomerZeroStory` | Problem / Solution / Impact narrative | `story_id`, `project_id`, `problem`, `solution`, `impact`, `metrics_ref`, `approval_status` |
| `Testimonial` | Feedback from stakeholders | `testimonial_id`, `source`, `role`, `quote`, `approval_status` |
| `Feedback` | Raw survey / listening evidence | `feedback_id`, `channel`, `sentiment`, `theme`, `period_id` |
| `QualitativeEvidence` | Normalized transcripts / quotes | `evidence_id`, `type`, `source_link`, `text_body`, `approved_for_reuse` |
| `GeneratedContent` | AI-produced drafts | `output_id`, `type`, `prompt_version`, `status`, `approver_id` |
| `Approval` | Review decisions | `approval_id`, `target_id`, `target_type`, `reviewer_id`, `decision`, `timestamp` |
| `RoleHubUsage` | Role Hub telemetry (curated) | `date`, `role`, `dau`, `mau`, `interactions` |
| `AgentUsage` | Agent telemetry (curated) | `date`, `agent_id`, `interactions`, `success_rate`, `avg_duration` |

### Relationships (summary)
- A `Project` has many `ProjectMember`, `Deliverable`, `ContentAsset`, `Metric`, `CustomerZeroStory`.
- A `TeamMember` participates in many `Project`s via `ProjectMember` with role + allocation.
- A `Metric` belongs to a `Project` and a `ReportingPeriod`, with a versioned formula.
- A `GeneratedContent` references the `Metric`s and `QualitativeEvidence` it was grounded in.
- Every `ContentAsset`, `CustomerZeroStory`, `Testimonial`, and `GeneratedContent` has an `Approval` trail.

---

## 7. File Storage Strategy

Use a **two-tier storage model** — the right file in the right place.

| File Type | Storage | Rationale |
|---|---|---|
| Office docs (PPTX, DOCX, XLSX, PDF) | **SharePoint** | Native M365 integration, sensitivity labels, versioning, Purview |
| Large media (video, high-res images) | **Azure Blob** | Cost-efficient, high throughput, Video Indexer integration |
| Generated outputs (QBR decks, drafts) | **SharePoint** (versioned) | Reviewers can co-author; Purview labels apply |
| Extracted text + embeddings | **Azure AI Search** | Hybrid vector + keyword retrieval |
| Structured metadata | **Dataverse** | Relational, secure, auditable |

### Upload Lifecycle

1. User uploads from within a Project → API receives file + metadata.
2. File lands in SharePoint (or Blob for large media) via a SAS-based direct upload.
3. Metadata row is written to Dataverse `ContentAsset` with `status = Uploaded`.
4. Event Grid emits `FileUploaded` → triggers the processing pipeline.
5. Once processed, `status → Indexed`. Once reviewed, `status → Approved`.
6. Only `Approved` content is visible to Copilot for external outputs.

---

## 8. File Processing Pipeline

An event-driven pipeline that turns raw uploads into AI-ready content.

```
File uploaded → Event Grid → Processing Function
                                    │
                                    ▼
1. Extract      (Document Intelligence, Video Indexer, OCR)
2. Normalize    (clean text, detect language, extract entities)
3. Chunk        (semantic chunking, 300–800 tokens, preserve boundaries)
4. Embed        (Azure OpenAI text-embedding-3-large)
5. Enrich       (auto-tag: theme, sentiment, entities; suggest links)
6. Publish      (write to Azure AI Search; flip status to Indexed)
```

### Chunking Rules
- 300–800 tokens per chunk with 10–15% overlap.
- Preserve slide / section / heading boundaries.
- Attach metadata to every chunk: `project_id`, `owner`, `quarter`, `sensitivity`, `approval_status`.

### Embedding Model
- Default: `text-embedding-3-large` (Azure OpenAI).
- Store vector + chunk text + metadata in Azure AI Search hybrid index.

---

## 9. AI Intelligence Layer

### 9.1 Retrieval-Augmented Generation (RAG)

All AI features (Ask Copilot, Content Studio, Analytics narratives, QBR generation) share a single retrieval brain.

**Hybrid retrieval:**
1. **Vector search** in Azure AI Search — semantic similarity.
2. **Keyword search** (BM25) — exact-term matches for names, product terms, metric labels.
3. **Structured lookup** in Dataverse — accurate KPI values with formulas.

Results are re-ranked and passed to the model with strict grounding instructions.

### 9.2 Ask Copilot Flow

```
User question
   │
   ▼
1. Query planner   — rewrite question, apply user permissions, apply context filters
2. Retrieval       — hybrid search (vector + keyword + structured)
3. Grounding       — build prompt with retrieved chunks + KPIs + instructions
4. Generation      — Azure OpenAI (GPT-4o) with citation format
5. Post-processing — verify citations, enforce approval gating, add source cards
6. Stream response — SSE to the frontend
```

### 9.3 Content Studio

Each content type is a **prompt template + output schema**.

| Content Type | Retrieval Scope | Output Shape | Guardrails |
|---|---|---|---|
| LinkedIn post | Approved metrics + approved quotes | Hook + metric + implication + CTA | External-Approved only |
| Viva Engage post | Internal wins + recognition | Recognition + shout-out | Internal-Approved OK |
| Executive summary | Top KPIs + top themes | 3 bullets: win / risk / ask | Cites every claim |
| QBR slide | Approved KPIs + narrative | Title, headline, 3 supporting points, chart hint | Formula version stamped |
| Case study | 1 Customer Zero + metrics | Problem / Solution / Impact / Quote | Requires External-Approved story |
| Blog | Multi-project theme | Long-form, sectioned | Editor review required |
| Newsletter | Quarter highlights | Modular sections | Internal-Approved OK |

Tone / Audience / Length selectors modify the prompt's instructions block only.

### 9.4 Analytics Intelligence
- **Narrative insights** — GPT-4o reads curated KPI tables and writes grounded explanations.
- **Anomaly detection** — scheduled Azure Function flags KPIs outside expected ranges.

### 9.5 AI Capabilities Summary

| Capability | Azure Service | Used By |
|---|---|---|
| Chat + reasoning | Azure OpenAI (GPT-4o / GPT-4.1) | Ask Copilot, Content Studio, exec summaries |
| Embeddings | Azure OpenAI (`text-embedding-3-large`) | RAG over uploaded content |
| Vector + keyword search | Azure AI Search | Retrieval brain |
| Document parsing | Azure AI Document Intelligence | PPT / PDF / DOCX / XLSX / images |
| Speech + video | Azure AI Video Indexer / Speech | Transcripts from listening sessions, huddles |
| Translation | Azure AI Translator | Multi-region evidence |
| Sentiment + key phrases | Azure AI Language | Testimonial themes |
| Content safety | Azure AI Content Safety | Guardrails on generated posts |
| Agent orchestration | Copilot Studio | Teams / Viva-native agent |
| Multi-step reasoning | Azure AI Foundry (Agents) | "Generate QBR" workflow |

---

## 10. API Surface

All APIs are thin, resource-oriented, and served via Azure API Management. Streaming endpoints use Server-Sent Events (SSE).

```
POST   /projects                          Create project
GET    /projects/{id}                     Get project detail
PATCH  /projects/{id}                     Update project
POST   /projects/{id}/members             Add member with role + allocation
GET    /projects/{id}/members             List members

POST   /projects/{id}/uploads             Upload file (SAS-based direct)
GET    /projects/{id}/assets              List assets
PATCH  /assets/{id}                       Update asset metadata
POST   /assets/{id}/approve               Approve asset

POST   /copilot/ask                       Streaming SSE
GET    /copilot/history/{userId}          Chat history

POST   /content-studio/generate           Streaming SSE
POST   /content-studio/{id}/approve       Approve generated content

GET    /analytics/kpis?period=FY26Q3      Curated KPIs
GET    /analytics/insights?scope=rolehub  AI-generated insights

POST   /qbr/generate?period=FY26Q3        Kick off QBR job
GET    /qbr/{id}/status                   Job status
GET    /qbr/{id}/download                 PPTX SAS URL

POST   /approvals                         Submit approval decision
GET    /approvals?status=pending          Reviewer inbox
```

### Auth
- All requests carry an Entra ID bearer token.
- APIM validates token, extracts claims, forwards user context.
- Function-level RBAC enforces group membership (Contributors / Stewards / Reviewers / Admins).

---

## 11. Access Model

Access is granted via **Microsoft 365 / Entra ID groups**, never individuals.

| Role | Group | Capabilities |
|---|---|---|
| **Contributor** | `AI-Impact-Contributors` | Upload evidence, submit contributions, view projects they're on |
| **Data Steward** | `AI-Impact-Stewards` | Manage benchmarks, curate evidence, validate uploads |
| **Reviewer** | `AI-Impact-Reviewers` | Approve content, sign off KPIs, gate external outputs |
| **Admin** | `AI-Impact-Admins` | Manage periods, schema, users, integrations |

Group membership is enforced end-to-end: APIM, Azure Functions, Dataverse security roles, SharePoint permissions.

---

## 12. Governance and Compliance

| Concern | Control |
|---|---|
| Data accuracy | Separate raw / curated / approved layers; formula versioning |
| Attribution | Cautious language ("estimated", "associated with"); every claim cited |
| Over-claiming | Human approval required before publish |
| External-safe content | External-Approved gate on LinkedIn / X / Blog outputs |
| Sensitivity | Microsoft Purview labels applied at SharePoint layer |
| Retention | SharePoint retention policies per library |
| Audit | Microsoft 365 audit log + Dataverse audit trail |
| DLP | Graph-level DLP policies on SharePoint |
| AI safety | Azure AI Content Safety on all generated outputs |

### Approval States
- `Draft` → `Internal Approved` → `External Approved`
- External surfaces (LinkedIn, X, Blog) only consume `External Approved` content.

---

## 13. Data Flow — End to End

```
1. Contributor uploads a file inside a Project
         │
         ▼
2. API stores metadata in Dataverse, file in SharePoint / Blob
         │
         ▼
3. Event Grid triggers processing pipeline
         │
         ▼
4. Extract → Normalize → Chunk → Embed → Enrich → Index
         │
         ▼
5. Asset visible in Knowledge Repository (status: Indexed)
         │
         ▼
6. Steward reviews and approves (status: Approved)
         │
         ▼
7. User asks Copilot / opens Content Studio / triggers QBR
         │
         ▼
8. Retrieval brain fetches approved chunks + KPIs
         │
         ▼
9. Azure OpenAI generates grounded, cited output
         │
         ▼
10. Reviewer approves output (status: Internal / External Approved)
         │
         ▼
11. Output delivered as PPTX / post draft / summary
```

---

## 14. Non-Functional Requirements

| Concern | Target |
|---|---|
| Availability | 99.9% for API tier |
| Latency (Copilot first token) | < 1.5s p95 |
| Latency (search) | < 300ms p95 |
| Upload size | Up to 2 GB (Blob), 250 MB (SharePoint) |
| Concurrent users | 500 (initial), 2,000 (target) |
| Cost visibility | Per-user, per-feature token dashboards |
| Observability | Application Insights + Log Analytics |
| Backup / restore | Dataverse native + SharePoint retention |
| Disaster recovery | Region-paired Azure resources |

---

## 15. Implementation Roadmap

### Phase 1 — Foundations (2–3 weeks)
- Entra ID auth + RBAC groups
- Dataverse schema (all core tables)
- SharePoint site + libraries (Inbox / Curated / Outputs)
- API skeleton (Azure Functions) + APIM
- Wire existing React frontend to real APIs

### Phase 2 — Upload + Processing Pipeline (2–3 weeks)
- SAS-based upload endpoint
- Event Grid → Document Intelligence → chunking → embeddings → Azure AI Search
- Auto-summary + auto-tagging at ingest
- Knowledge Repository backed by real data

### Phase 3 — Ask Copilot (2 weeks)
- RAG service with hybrid retrieval
- Grounded prompt + citations + streaming (SSE)
- Feedback capture (👍 / 👎)
- Content Safety guardrails

### Phase 4 — Content Studio + Analytics AI (2–3 weeks)
- Prompt templates per content type
- External-Approved gating
- Analytics narrative generation
- Insights panel

### Phase 5 — QBR Generation + Governance (2–3 weeks)
- Structured evidence packager
- Slide assembler → PPTX (Graph API or `python-pptx` in container)
- Approval workflow (Power Automate)
- Full audit trail + version history

### Phase 6 — Production Hardening (2 weeks)
- Observability, cost dashboards, rate limits
- DLP + Purview end-to-end
- Load testing, failover, backup/restore
- Runbooks + admin console

**Estimated total:** ~13–16 weeks with a small dev team (2–3 engineers).

---

## 16. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Azure OpenAI quota limits | Feature outages | Request tenant quota early; add fallback model |
| Hallucinated content | Trust erosion | Strict grounding + citations + human approval gate |
| Upload governance drift | Non-compliant content in outputs | Sensitivity labels + external-approved gating |
| KPI inconsistency | Executive credibility | Formula versioning + confidence flags |
| Cost overruns | Budget risk | Per-user quotas + caching + token dashboards |
| Adoption lag | ROI erosion | Contribution submission form + Teams integration + reminders |

---

## 17. Glossary

| Term | Definition |
|---|---|
| **RAG** | Retrieval-Augmented Generation — grounding LLM responses in retrieved documents |
| **KPI** | Key Performance Indicator, calculated with a versioned formula |
| **Curated data** | Cleaned, normalized data derived from raw sources |
| **Evidence package** | Structured JSON containing KPIs + approved quotes + themes handed to the model |
| **External-Approved** | Content cleared for external channels (LinkedIn, blog, X) |
| **Customer Zero** | Internal customer story showcasing early adoption of an AI capability |
| **QBR** | Quarterly Business Review deck for leadership |
| **Copilot Studio** | Microsoft's platform for building custom copilots and agents |

---

## 18. Appendix — Reference Diagrams

### Layered Architecture

```
┌────────────────────────────────────────────┐
│ Presentation      React SPA (Fluent 2)     │
├────────────────────────────────────────────┤
│ API Gateway       Azure API Management     │
├────────────────────────────────────────────┤
│ Services          Azure Functions          │
│                   (Projects, Copilot,      │
│                    Content, Analytics)     │
├────────────────────────────────────────────┤
│ Intelligence      Azure OpenAI +           │
│                   Azure AI Search          │
├────────────────────────────────────────────┤
│ Processing        Event Grid + Functions + │
│                   Document Intelligence    │
├────────────────────────────────────────────┤
│ Data              Dataverse · SharePoint · │
│                   Azure Blob               │
├────────────────────────────────────────────┤
│ Identity          Entra ID + Groups        │
└────────────────────────────────────────────┘
```

### Approval Lifecycle

```
 Upload ──► Indexed ──► Internal Approved ──► External Approved
                             │                       │
                             ▼                       ▼
                    Internal QBR / Viva     LinkedIn / Blog / X
```

---

**End of document.**
