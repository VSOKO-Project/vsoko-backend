using coo.Domain.Entities;
using coo.Infrastructure.DataManager.Configurations;
using coo.Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore; 

namespace coo.Infrastructure.DataManager.Contexts;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }

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
    }
 
}