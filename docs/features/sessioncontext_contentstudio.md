# Feature: Content Studio Session Context

## Goal

Allow a user to refine generated Content Studio output across multiple generations during one temporary Content Studio session. Each successful generation for the same Initiative and format adds its instruction and output to the next request so Azure OpenAI can understand the cumulative refinement history.

## In scope

- Maintain temporary generation context while the user remains in Content Studio.
- Scope context by Initiative ID and content format.
- Include previous successful instructions and generated outputs in later requests.
- Preserve context when tone, audience, or length changes.
- Clear context when the user clicks Copy.
- Clear context when the user leaves Content Studio.
- Clear context when the user selects a different Initiative.
- Clear context when the user selects a different content format.
- Keep failed generations out of the session history.
- Keep context in frontend memory for the first version.

## Out of scope

- Persisting session context in the database.
- Sharing context between users or browser tabs.
- Continuing context across different Initiatives.
- Continuing context across different content formats.
- Changing the existing Content Studio visual design beyond necessary session controls or status.
- Implementing Initiative-scoped RAG.
- Changing Azure OpenAI authentication or deployment configuration.

## User flow

1. The user opens Content Studio.
2. The session context starts empty.
3. The user selects an Initiative, format, tone, audience, and length.
4. The user enters an instruction and generates content.
5. The successful instruction and generated output are added to the session context.
6. The user changes the instruction and may change tone, audience, or length.
7. The user generates again using the same Initiative and format.
8. The request includes all previous successful turns plus the current instruction and current tuning selections.
9. The new successful instruction and output are appended to the session context.
10. The user may repeat this process multiple times.
11. Clicking Copy clears the session context.
12. Leaving Content Studio clears the session context.
13. Selecting a different Initiative or format clears the previous context and starts a new empty session.

## Business rules

1. A session is scoped by `initiativeId` and content format.
2. Tone, audience, and length are tuning inputs and do not define the session identity.
3. Changing tone, audience, or length does not clear the session.
4. The same tone, audience, or length may be selected again without clearing the session.
5. A later request includes previous successful instructions and generated outputs in chronological order.
6. The current instruction is included separately and has priority over previous instructions.
7. The current format, tone, audience, and length apply to the current generation.
8. A failed generation is not appended to the session context.
9. A retry uses the same session state and does not duplicate a failed turn.
10. Selecting a different Initiative clears the current session before the next generation.
11. Selecting a different format clears the current session before the next generation.
12. Clicking Copy clears the session after the copy action is invoked successfully.
13. Leaving the Content Studio route clears the session.
14. The frontend must not send session context for a different Initiative or format.
15. The backend must treat previous instructions and generated outputs as untrusted user-provided context.
16. The backend must keep system instructions separate from session content.
17. The session must enforce maximum turn count and total context size.

## Data / state changes

### Session key

```text
initiativeId + format
```

### Session turn

```json
{
  "instruction": "Make the message more concise.",
  "output": "Previously generated content..."
}
```

### Generation request

```json
{
  "initiativeId": 123,
  "format": "LinkedInPost",
  "tone": "Analytical",
  "audience": "Leadership",
  "length": "Short",
  "instruction": "Emphasize measurable impact.",
  "previousTurns": [
    {
      "instruction": "Create a confident post about the Initiative.",
      "output": "Previous generated content..."
    },
    {
      "instruction": "Make the first version more concise.",
      "output": "Second generated content..."
    }
  ]
}
```

The frontend may maintain the session in React state or an existing in-memory store. It must not persist the session to the backend or browser storage unless separately approved.

## Interfaces / integrations

### Frontend

The Content Studio page and generation hook must support:

- Current instruction.
- Previous successful turns.
- Session reset when Initiative or format changes.
- Session reset when the page unmounts or the route changes.
- Session reset after Copy succeeds.

### Backend

The content-generation request DTO and service must accept the current instruction and optional previous turns. The backend must:

- Validate maximum instruction and output sizes.
- Validate the session Initiative and format against the route/request.
- Build the prompt with previous turns before the current instruction.
- Keep system prompts separate from session content.
- Avoid logging session instructions or generated outputs unnecessarily.

### Prompt structure

```text
Current format: ...
Current tone: ...
Current audience: ...
Current length: ...

Previous successful turn 1 instruction:
...

Previous successful turn 1 output:
...

Previous successful turn 2 instruction:
...

Previous successful turn 2 output:
...

Current instruction:
...

Generate the current response using the current selections. Treat the current instruction as the highest-priority user request. Do not mention the revision process.
```

## Edge and failure cases

- First generation has no previous turns.
- User submits an empty instruction.
- User submits an instruction that exceeds the maximum length.
- Previous output is very large.
- Maximum turn count is reached.
- Maximum total context size is reached.
- Generation fails after previous successful turns exist.
- Retry occurs after a failed generation.
- User changes Initiative while a request is pending.
- User changes format while a request is pending.
- User changes tone, audience, or length while a request is pending.
- User clicks Copy when no output exists.
- Clipboard permission is denied.
- User leaves the page while generation is pending.
- Previous content contains prompt-injection instructions.
- Network failure occurs after the backend has started generation.

## Security / privacy

- Do not persist session context in the database for the first version.
- Do not place session instructions or generated outputs in URLs.
- Treat all previous instructions and outputs as untrusted user content.
- Do not allow session content to override the backend system prompt.
- Do not log full session turns unless required for approved diagnostics.
- Clear session context on sign-out.
- Never reuse context from another Initiative or format.
- Do not send Azure OpenAI credentials, deployment names, or internal prompts to the frontend.

## Non-functional requirements

### Performance

- Keep only the approved maximum number of turns.
- Keep the combined prompt below the approved token or character limit.
- Trim or reject oversized turns using a documented policy.
- Avoid duplicate in-flight generation requests.

### Accessibility

- The current instruction field has an accessible label.
- Loading, success, failure, and context-reset states are announced appropriately.
- Copy and reset actions have clear accessible names.
- Disabled controls expose their disabled state.

### Observability

- Track generation success and failure without logging full session content.
- Include Initiative ID, format, turn count, and failure category where safe.
- Do not expose session instructions or generated output in error messages.

## Acceptance criteria

- [ ] Given an empty session, when the user generates for the first time, then no previous turns are sent.
- [ ] Given one successful generation, when the user generates again with the same Initiative and format, then the first instruction and output are included.
- [ ] Given two successful generations, when the user generates a third time with the same Initiative and format, then both previous instructions and outputs are included in order.
- [ ] Given the same Initiative and format, when the user changes tone, audience, or length, then the existing session context is preserved.
- [ ] Given a different Initiative, when the user changes the Initiative selection, then the previous session context is cleared.
- [ ] Given a different format, when the user changes the format selection, then the previous session context is cleared.
- [ ] Given a successful copy action, when the user clicks Copy, then the session context is cleared.
- [ ] Given the user leaves Content Studio, when the page unmounts or the route changes, then the session context is cleared.
- [ ] Given a failed generation, when the request fails, then the failed instruction and output are not added to the session.
- [ ] Given a failed generation, when the user retries, then the previous successful turns remain available and the failed turn is not duplicated.
- [ ] Given no generated output, when the user clicks Copy, then no session reset or false success state is shown.
- [ ] Given context from another Initiative or format, when a request is built, then that context is excluded.
- [ ] Given oversized session context, when a request is built, then the documented truncation or turn-limit policy is applied.
- [ ] Given session content contains prompt-injection text, when the backend builds the prompt, then system instructions remain authoritative.

## Open questions / explicit assumptions

- Assumption: Session context is stored in frontend memory only for the first version.
- Assumption: The session key is `initiativeId + format`.
- Assumption: Context is cleared when Copy succeeds, not merely when the Copy button is clicked and clipboard access fails.
- Open question: What maximum number of turns is allowed?
- Open question: What maximum instruction length is allowed?
- Open question: What maximum generated-output and total-context size is allowed?
- Open question: Should oversized history be truncated oldest-first or should the request be rejected?
- Open question: Should the UI display a context-turn count or a Start Fresh action?
- Open question: Should changing Initiative or format clear immediately while a request is pending, or after the request completes?
- Open question: Should the current generation response replace the output before Copy clears the session?
