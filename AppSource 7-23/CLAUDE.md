# Frontend Development Instructions

## Project

This is the Team Intelligence Hub frontend application.

The application uses:
- React
- TypeScript
- Vite
- npm/Bun

## Architecture

Before making changes:
- Inspect the existing project structure.
- Follow the existing component and folder conventions.
- Reuse existing components, hooks, utilities, and services where possible.
- Do not introduce new dependencies unless necessary.

## Sensitive Files — DO NOT ACCESS

The following files may contain secrets, API keys, tokens, credentials, private URLs, or environment-specific configuration.

NEVER:
- Read these files
- Display their contents
- Copy their contents
- Log their contents
- Commit their contents
- Modify them unless explicitly instructed

Sensitive files:

- `.env`
- `.env.local`
- `.env.development`
- `.env.development.local`
- `.env.production`
- `.env.production.local`
- `.env.test`
- `.env.test.local`

Also treat any file matching:

- `.env.*`
- `**/secrets.*`
- `**/*secret*`
- `**/*credentials*`

as sensitive.

## Environment Variables

Use `.env.example` as the reference for available environment variables.

If an environment variable is required, refer to it by its variable name only.

For example:

`VITE_API_BASE_URL`

Do NOT request or expose the actual value from `.env.local`.

Use placeholders when discussing configuration:

`<API_URL>`
`<API_KEY>`
`<SECRET>`

## Security

Never place secrets directly into:
- React components
- TypeScript source files
- JavaScript source files
- `vite.config.ts`
- `package.json`
- documentation
- Git commits

Never output API keys, access tokens, passwords, connection strings, or other credentials in responses.

## Development Rules

- Keep frontend code inside the existing project architecture.
- Follow existing TypeScript conventions.
- Prefer existing UI components before creating new ones.
- Keep components focused and reusable.
- Do not modify unrelated files.
- Do not expose environment secrets to the browser unnecessarily.

## Before Making Changes

First inspect the relevant source files and understand the existing implementation.

Do not inspect sensitive environment files.

When configuration is needed, use `.env.example` or ask for the environment variable NAME, not its secret VALUE.