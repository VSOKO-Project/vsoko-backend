using Application.Interfaces.FileManager;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.FileManager;

public static class DI
{
    public static IServiceCollection ApplyFileManager(this IServiceCollection services)
    {
        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<RosterFile>();
        services.AddSingleton<IRosterFileReader>(sp => sp.GetRequiredService<RosterFile>());
        services.AddSingleton<IRosterFileWriter>(sp => sp.GetRequiredService<RosterFile>());
        return services;
    }
}