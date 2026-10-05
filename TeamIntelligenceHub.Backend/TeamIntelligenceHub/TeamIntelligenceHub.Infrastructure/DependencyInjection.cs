// 1. Register the SQL Server database context
// 2. Register the user and initiative repositories and services
// 3. Register the initiative member and task repositories and services
// 4. Register the task comment and comment attachment repositories and services
// 5. Register the activity, contribution, and contribution attachment repositories and services
// 6. Register the content generation service
// 7. Bind the blob storage options and register file storage
// 8. Register the document insight extraction queue
// 9. Bind the Azure OpenAI and Azure AI Search options
// 10. Register the embedding, vector search, and chat clients and the Copilot service
// 11. Register the document testimonial and customer story repository

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

/// <summary>
/// Registers the database context, repositories, application services, file storage, queue,
/// and Azure AI clients with the service collection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Infrastructure layer's database context, repositories, application services,
    /// file storage, extraction queue, and Azure AI clients.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The application configuration that supplies the connection string and option sections.</param>
    /// <returns>The same service collection, so further calls can be chained.</returns>
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

        // Generates content through IChatCompletionClient, which is registered below.
        services.AddScoped<IContentGenerationService, ContentGenerationService>();

        // Bound from the "BlobStorage" section, which resolves from appsettings.json,
        // user secrets, or environment variables without a code change.
        services.Configure<BlobStorageOptions>(
            configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton<IFileStorage, AzureBlobFileStorage>();

        services.AddSingleton<
            IDocumentInsightExtractionQueue, StorageQueueDocumentInsightExtractionQueue>();

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

        // Data access only. The extraction workflow (search, prompting, parsing) lives in
        // the standalone TestimonialAndCustomerStoryExtractionFunction project. This
        // repository backs ContributionService's read side for the Stories & Evidence
        // page's "Extracted from documents" section.
        services.AddScoped<
            IDocumentTestimonialAndCustomerStoryRepository,
            DocumentTestimonialAndCustomerStoryRepository>();

        return services;
    }
}
