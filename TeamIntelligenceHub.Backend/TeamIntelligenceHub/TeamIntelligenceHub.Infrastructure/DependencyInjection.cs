using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Infrastructure.Persistence;
using TeamIntelligenceHub.Infrastructure.Persistence.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Application.Services;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Infrastructure.AI;
using TeamIntelligenceHub.Infrastructure.Storage;

namespace TeamIntelligenceHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TeamIntelligenceHubDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString(
                    "DefaultConnection"));
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IInitiativeRepository, InitiativeRepository>();
        services.AddScoped<IInitiativeService, InitiativeService>();

        services.AddScoped<IInitiativeMemberRepository, InitiativeMemberRepository>();
        services.AddScoped<IInitiativeMemberService, InitiativeMemberService>();

        services.AddScoped<IInitiativeTaskRepository, InitiativeTaskRepository>();
        services.AddScoped<IInitiativeTaskService, InitiativeTaskService>();

        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<ITaskCommentService, TaskCommentService>();

        services.AddScoped<
            ITaskCommentAttachmentRepository, TaskCommentAttachmentRepository>();
        services.AddScoped<
            ITaskCommentAttachmentService, TaskCommentAttachmentService>();

        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IActivityService, ActivityService>();

        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IContributionService, ContributionService>();

        services.AddScoped<
            IContributionAttachmentRepository, ContributionAttachmentRepository>();
        services.AddScoped<
            IContributionAttachmentService, ContributionAttachmentService>();

        // Structured-data only for now — no Azure OpenAI dependency yet, unlike
        // ICopilotService below. That lands in a later slice.
        services.AddScoped<IContentGenerationService, ContentGenerationService>();

        // Bound from the "BlobStorage" section, which resolves from appsettings.json,
        // user secrets, or environment variables without a code change.
        services.Configure<BlobStorageOptions>(
            configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();

        // Bound the same way as BlobStorage: appsettings.json/user secrets/environment
        // variables all work without a code change.
        services.Configure<AzureOpenAiOptions>(
            configuration.GetSection(AzureOpenAiOptions.SectionName));
        services.Configure<AzureAiSearchOptions>(
            configuration.GetSection(AzureAiSearchOptions.SectionName));

        services.AddSingleton<IEmbeddingClient, AzureOpenAiEmbeddingClient>();
        services.AddSingleton<IVectorSearchClient, AzureAiSearchVectorClient>();
        services.AddSingleton<IChatCompletionClient, AzureOpenAiChatClient>();
        services.AddScoped<ICopilotService, CopilotService>();

        return services;
    }
}