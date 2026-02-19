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
        var conectionstring = configuration.GetConnectionString("Database");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conectionstring));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ICriteriaRepository, CriteriaRepository>();
        services.AddScoped<IDisciplineRepository, DisciplineRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<IWorkloadRepository, WorkloadRepository>();

        // Mappers
        services.AddSingleton<Application.Common.Mappings.CriteriaMapper>();
        services.AddSingleton<Application.Common.Mappings.DisciplineMapper>();
        services.AddSingleton<Application.Common.Mappings.FeedbackMapper>();
        services.AddSingleton<Application.Common.Mappings.TeacherMapper>();
        services.AddSingleton<Application.Common.Mappings.WorkloadMapper>();

        return services;
    }
}
