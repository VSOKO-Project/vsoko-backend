using Application.Interfaces.FileManager;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.FileManager;

public static class DI
{
    public static IServiceCollection ApplyFileManager(this IServiceCollection services)
    {
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}