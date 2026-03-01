using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Application.Services.BatchJobs;

namespace Valora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IContextReportService, ContextReportService>();
        services.AddScoped<IContextAnalysisService, ContextAnalysisService>();
        services.AddScoped<IUserAiProfileService, UserAiProfileService>();
        services.AddScoped<IContextDataProvider, ContextDataProvider>();
        services.AddScoped<IMapService, MapService>();
        services.AddScoped<IBatchJobService, BatchJobService>();
        services.AddScoped<IBatchJobExecutor, BatchJobExecutor>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
        services.AddScoped<IWorkspacePropertyService, WorkspacePropertyService>();

        // Batch Job Processors
        services.AddScoped<IBatchJobProcessor, CityIngestionJobProcessor>();
        services.AddScoped<IBatchJobProcessor, MapGenerationJobProcessor>();
        services.AddScoped<IBatchJobProcessor, AllCitiesIngestionJobProcessor>();

        return services;
    }
}
