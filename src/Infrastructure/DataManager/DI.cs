using Application.Interfaces.DataManager.Repositories;
using Application.Interfaces.DataManager;
using Infrastructure.DataManager.Contexts;
using Infrastructure.DataManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.Replication;

namespace Infrastructure.DataManager;

public static class DI
{
    public static IServiceCollection ApplyDataManager(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not found.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICriteriaRepository, CriteriaRepository>();
        services.AddScoped<IDisciplineRepository, DisciplineRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<IWorkloadRepository, WorkloadRepository>();

        services.AddSingleton<Application.Common.Mappings.CriteriaMapper>();
        services.AddSingleton<Application.Common.Mappings.DisciplineMapper>();
        services.AddSingleton<Application.Common.Mappings.FeedbackMapper>();
        services.AddSingleton<Application.Common.Mappings.TeacherMapper>();
        services.AddSingleton<Application.Common.Mappings.WorkloadMapper>();

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "PostgreSQL Database",
                healthQuery: "SELECT 1;",
                tags: new[] { "db", "ready" },
                timeout: TimeSpan.FromSeconds(3)
            );
        
        return services;
    }
}
