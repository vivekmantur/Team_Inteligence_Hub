# TeamIntelligenceHub Development Instructions

## Project Structure

This is a .NET solution containing:

- TeamIntelligenceHub.API
- TeamIntelligenceHub.Application
- TeamIntelligenceHub.Domain
- TeamIntelligenceHub.Infrastructure

Follow the existing Clean Architecture structure.

## Sensitive Files

The following files contain secrets, credentials, connection strings, or environment-specific configuration.

DO NOT read, inspect, modify, or expose the contents of these files:

- TeamIntelligenceHub.API/appsettings.json
- TeamIntelligenceHub.API/appsettings.*.json
- **/.env
- **/.env.*
- **/secrets.json

Never output connection strings, API keys, passwords, tokens, or other secrets in responses.

If configuration values are required for development, ask me for the required setting name and use a placeholder such as:

`<CONNECTION_STRING>`

instead of requesting or displaying the actual secret.

## Coding Rules

- Follow the existing project architecture.
- Keep API-specific code in TeamIntelligenceHub.API.
- Keep business logic in TeamIntelligenceHub.Application.
- Keep domain entities and domain logic in TeamIntelligenceHub.Domain.
- Keep database/external infrastructure implementations in TeamIntelligenceHub.Infrastructure.
- Reuse existing services and patterns before creating new ones.
- Do not introduce unnecessary dependencies.
- Do not modify configuration files containing secrets unless explicitly instructed.