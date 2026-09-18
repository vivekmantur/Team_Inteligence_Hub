using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestimonialAndCustomerStoryExtractionFunction;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

// Same database the main API reads and writes — "ConnectionStrings:DefaultConnection",
// matching the main backend's own config section name so the same local.settings.json
// values work without renaming anything. This project has no reference to the main
// backend, though: ExtractionDbContext is its own local model, scoped to just the two
// tables this Function touches.
builder.Services.AddDbContext<ExtractionDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<AzureOpenAiOptions>(
    builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));
builder.Services.Configure<AzureAiSearchOptions>(
    builder.Configuration.GetSection(AzureAiSearchOptions.SectionName));

builder.Services.AddSingleton<EmbeddingClient>();
builder.Services.AddSingleton<DocumentSearchClient>();
builder.Services.AddSingleton<ChatCompletionClient>();

builder.Services.AddScoped<TestimonialAndCustomerStoryExtractor>();

builder.Build().Run();
