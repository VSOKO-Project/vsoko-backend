using coo.Domain.Entities;
using coo.Infrastructure.DataManager.Common;
using coo.Infrastructure.SecurityManager;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using coo.Infrastructure.SecurityManager.AspNetCoreIdentity;

namespace coo.Infrastructure.DataManager.Configurations;

public class EmployeeConfiguration : BaseEntityConfiguration<Employee>
{
    public override void Configure(EntityTypeBuilder<Employee> builder)
    {
        base.Configure(builder);

        builder.HasOne(w => w.RoleRef)
        .WithMany(w => w.EmployeeRefs)
        .HasForeignKey(w => w.RoleId);

        builder.HasOne<ApplicationUser>()
        .WithOne(w => w.EmployeeRef)
        .HasForeignKey<Employee>(w => w.Id);
    }
}