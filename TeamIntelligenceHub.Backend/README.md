# TeamIntelligenceHub.Backend

ASP.NET Core 9 Web API behind Microsoft Entra ID, serving the Team Intelligence Hub SPA.

## Configuration — keep ids out of the repo

`appsettings.json` ships with empty values on purpose. It is tracked by git, so nothing
sensitive goes in it. Real values live in **user secrets**, stored in your Windows profile
under `%APPDATA%\Microsoft\UserSecrets\`, outside the repository entirely.

Set them once, from `TeamIntelligenceHub/TeamIntelligenceHub.API`:

```bash
dotnet user-secrets set "AzureAd:TenantId" "<directory-tenant-id>"
```

```bash
dotnet user-secrets set "AzureAd:ClientId" "<application-client-id>"
```

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-sql-connection-string>"
```

`ClientId` must be the same app registration the SPA signs in with — it is the audience the
`api://<client-id>/access_as_user` access tokens are issued for.

Read them back any time with `dotnet user-secrets list`. Configuration layering means secrets
override `appsettings.json` in Development without any code change.

For deployment, supply the same keys as environment variables or from Azure Key Vault. The
double-underscore form maps onto the nested keys: `AzureAd__ClientId`.

## Running

```bash
dotnet ef database update --project TeamIntelligenceHub.Infrastructure --startup-project TeamIntelligenceHub.API
```

```bash
dotnet run --project TeamIntelligenceHub.API --launch-profile http
```

The API listens on `http://localhost:5103`. HTTPS redirection is disabled in Development so the
SPA can call that origin directly — a cross-origin preflight cannot follow the 307 to https.

## CORS

`Cors:AllowedOrigins` in `appsettings.json` lists the origins allowed to call the API. These are
not secrets, so they stay in the tracked file. Add the deployed SPA URL there before shipping.

## User provisioning

`GET /api/users/me` is the endpoint the SPA calls right after sign-in. It reads `oid`,
`preferred_username`, and `name` from the access token, creates the `Users` row on first sign-in,
and refreshes the profile plus `LastLoginAt` on every call after that. No separate signup step
exists — the first successful sign-in is the signup.
