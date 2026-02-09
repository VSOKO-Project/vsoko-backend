using Domain.Entities;
using Infrastructure.DataManager.Common;
using Infrastructure.SecurityManager;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Infrastructure.SecurityManager.AspNetCoreIdentity;

namespace Infrastructure.DataManager.Configurations;

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