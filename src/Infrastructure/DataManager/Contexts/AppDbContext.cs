using System.Linq.Expressions;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataManager.Configurations;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Infrastructure.DataManager.Contexts;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<StudentGroup> StudentGroups { get; set; }
    public DbSet<Discipline> Disciplines { get; set; }
    public DbSet<EmployeeRole> EmployeeRoles { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Criteria> Criterias { get; set; }
    public DbSet<CriteriaFeedback> criteriaFeedbacks { get; set; }
    public DbSet<Workload> Workloads { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Refresh> Refreshes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());

        modelBuilder.ApplyConfiguration(new StudentGroupConfiguration());
        modelBuilder.ApplyConfiguration(new DisciplineConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeRoleConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new CriteriaConfiguration());
        modelBuilder.ApplyConfiguration(new CriteriaFeedbackConfiguration());
        modelBuilder.ApplyConfiguration(new WorkloadConfiguration());
        modelBuilder.ApplyConfiguration(new FeedbackConfiguration());
        modelBuilder.ApplyConfiguration(new TeacherConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshConfiguration());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder
                    .Entity(entityType.ClrType)
                    .HasQueryFilter(
                        ConvertFilterExpression<BaseEntity>(e => !e.IsDeleted, entityType.ClrType)
                    );
            }
        }
    }

    private static LambdaExpression ConvertFilterExpression<TInterface>(
        Expression<Func<TInterface, bool>> filterExpression,
        Type entityType
    )
    {
        var newParam = Expression.Parameter(entityType);
        var newBody = ReplacingExpressionVisitor.Replace(
            filterExpression.Parameters.Single(),
            newParam,
            filterExpression.Body
        );
        return Expression.Lambda(newBody, newParam);
    }
}
