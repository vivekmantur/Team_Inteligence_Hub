# TestimonialAndCustomerStoryExtractionFunction Development Instructions

## What this project is

A standalone Azure Function App (Consumption, .NET 9 isolated worker), separate from the
main `TeamIntelligenceHub.Backend` solution. It is queue-triggered: a message carrying a
`ContributionAttachmentId` arrives on the `testimonial-customer-story-extraction` Storage
Queue, and the Function checks that attachment's indexed document content for a genuine
testimonial and/or customer story, saving whatever it finds into the
`DocumentTestimonialsAndCustomerStories` table.

Nothing calls this Function directly and it exposes no HTTP endpoint — the queue is its
only trigger, and the database write is its only externally visible effect. Reading that
data back out for the frontend is the main API's job, not this project's.

## Project structure

- `TestimonialAndCustomerStoryExtractionFunction.cs` — the queue trigger. Parses the
  message as a `ContributionAttachmentId` and calls the extractor. Nothing else.
- `TestimonialAndCustomerStoryExtractor.cs` — the extraction workflow: look up the
  attachment, search for a customer story and a testimonial independently (each is a
  separate embed → scoped hybrid search → LLM call → parse → save pass), log the outcome.
- `ExtractionPromptBuilder.cs` — the two fixed system/user prompts (customer story,
  testimonial). Pure, no I/O.
- `ExtractionResultParser.cs` — parses the model's plain-text labeled-line response back
  into typed data (`IChatCompletionClient` has no structured/JSON output mode).
- `Program.cs` — DI wiring: `TeamIntelligenceHubDbContext`, the three AI client
  interfaces, and the two repositories, all reused from the main backend.

## Architectural boundary — read before adding anything here

This project references `TeamIntelligenceHub.Domain` and `TeamIntelligenceHub.Infrastructure`
from the main backend (`..\TeamIntelligenceHub.Backend\TeamIntelligenceHub\`), but
deliberately **not** `TeamIntelligenceHub.Application`. That split is intentional, not an
oversight:

- **Reused from the main backend**: entities/enums (`Domain`), and pure data access plus
  generic AI-client implementations (`Infrastructure`) — `IContributionAttachmentRepository`,
  `IDocumentTestimonialAndCustomerStoryRepository`, `IEmbeddingClient`,
  `IVectorSearchClient`, `IChatCompletionClient`. These are thin, generic abstractions,
  not business logic, and the main API also depends on them for its own purposes.
- **Lives only here**: the actual extraction workflow — which queries to run, the
  prompts, how a response is parsed, what counts as "genuine enough to save". This was a
  deliberate choice: keep this Function fully self-contained rather than making it a thin
  wrapper around a shared Application-layer service. Do not move this logic into
  `TeamIntelligenceHub.Application`, and do not add a `ProjectReference` to that project
  from here — if a future change seems to need something from it, that is a sign the
  logic belongs in a repository or a genuinely generic interface in `Infrastructure`
  instead, not a reason to cross this boundary.

## Sensitive files

The following contain secrets, credentials, connection strings, or
environment-specific configuration.

DO NOT read, inspect, modify, or expose the contents of these files:

- `TestimonialAndCustomerStoryExtractionFunction/local.settings.json`
- Anything under this project's user secrets store (the `UserSecretsId` in the `.csproj`
  points at a `secrets.json` outside the repo, under the user profile)
- `**/.env`
- `**/.env.*`

Never output connection strings, API keys, passwords, tokens, or other secrets in
responses.

If a configuration value is needed for development, ask for the required setting name
and use a placeholder such as `<CONNECTION_STRING>` instead of requesting or displaying
the actual secret. `local.settings.json` needs `ConnectionStrings:DefaultConnection`,
`AzureOpenAI:*`, and `AzureAiSearch:*` alongside the existing `AzureWebJobsStorage`/
`StorageConnection` entries — same setting names and shape as the main backend's own
`appsettings.json`, which is itself off-limits for the same reason.

## Coding rules

- Follow the existing pattern in each file rather than introducing a new one — the
  prompt builder stays pure, the parser stays a plain-text line parser (not JSON, since
  `IChatCompletionClient` doesn't support structured output), the extractor stays the
  only place orchestration happens.
- Do not add background/scheduled triggers to this Function App — it is queue-triggered
  only, by design (see the main repo's `docs/features` for why: Azure AI Search indexing
  happens out-of-band with unknown lag, which is what the queue's delayed message
  visibility is meant to cover).
- Do not introduce unnecessary dependencies.
- Do not modify configuration files containing secrets unless explicitly instructed.
