// 1. Check that the Azure AD tenant and client IDs are configured
// 2. Register the CORS policy for the SPA origins
// 3. Register controllers with enum names in JSON
// 4. Register the current user service
// 5. Add Microsoft Entra ID bearer token authentication
// 6. Log the reason a bearer token is rejected
// 7. Add authorization
// 8. Register the infrastructure services
// 9. Configure Swagger with the OAuth2 PKCE and bearer token schemes
// 10. Enable Swagger UI in Development, or HTTPS redirection elsewhere
// 11. Add the CORS, authentication, and authorization middleware
// 12. Map the controllers and run the app

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using TeamIntelligenceHub.Infrastructure;
using TeamIntelligenceHub.API.Services;
using TeamIntelligenceHub.Application.Interfaces;

var builder = WebApplication.CreateBuilder(args);

const string SpaCorsPolicy = "SpaCorsPolicy";

// Fail loudly at startup. Without these the API still starts but rejects every token,
// which shows up as an unexplained 401 in the browser.
var azureAd = builder.Configuration.GetSection("AzureAd");

if (string.IsNullOrWhiteSpace(azureAd["TenantId"]) ||
    string.IsNullOrWhiteSpace(azureAd["ClientId"]))
{
    throw new InvalidOperationException(
        "AzureAd:TenantId and AzureAd:ClientId are not configured, so every request " +
        "would fail with 401. Set them from TeamIntelligenceHub.API with: " +
        "dotnet user-secrets set \"AzureAd:TenantId\" \"<directory-tenant-id>\" and " +
        "dotnet user-secrets set \"AzureAd:ClientId\" \"<application-client-id>\". " +
        "See README.md in the backend root.");
}

// Origins the SPA is served from. Add the deployed app URL here before shipping.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(SpaCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Send and accept enum names ("OnTrack") rather than ordinals, so the API
        // contract stays readable and is not tied to declaration order.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"));

// Surface why a token was rejected. Microsoft.Identity.Web installs its own handlers,
// so chain onto them rather than replacing them.
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var inner = options.Events?.OnAuthenticationFailed;

        options.Events ??= new JwtBearerEvents();

        options.Events.OnAuthenticationFailed = async context =>
        {
            if (inner is not null)
            {
                await inner(context);
            }

            context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("TeamIntelligenceHub.Auth")
                .LogWarning(
                    context.Exception,
                    "Bearer token rejected: {Reason}",
                    context.Exception.Message);
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddInfrastructure(
    builder.Configuration);

// Lets Swagger's "Authorize" button run the Authorization Code + PKCE flow a real client
// uses, so testers need not mint tokens by hand. The scope must be exposed by the
// AzureAd:ClientId app registration; set AzureAd:SwaggerScope to override "access_as_user".
var tenantId = azureAd["TenantId"]!;
var apiClientId = azureAd["ClientId"]!;
var swaggerScope = azureAd["SwaggerScope"] ?? $"api://{apiClientId}/access_as_user";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    [swaggerScope] = "Call the API as the signed-in user"
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "oauth2"
            }
        }] = new[] { swaggerScope }
    });

    // Lets testers paste a token they already hold (e.g. copied from the SPA) instead of
    // running the oauth2 flow; the API validates it the same way either way. A separate
    // AddSecurityRequirement makes the two schemes alternatives rather than both required.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste an already-obtained access token (without the \"Bearer \" prefix)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // A public client: Swagger runs entirely in the browser and cannot keep a
        // client secret, so PKCE takes its place.
        options.OAuthClientId(apiClientId);
        options.OAuthUsePkce();
        options.OAuthScopeSeparator(" ");
    });
}
else
{
    // Skipped in Development so the SPA can call the plain http profile without
    // a 307 to https, which a cross-origin preflight cannot follow.
    app.UseHttpsRedirection();
}

// Must run before authentication so preflight requests get their headers.
app.UseCors(SpaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
