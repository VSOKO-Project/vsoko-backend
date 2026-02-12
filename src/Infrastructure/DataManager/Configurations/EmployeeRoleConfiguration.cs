using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class EmployeeRoleConfiguration : BaseEntityConfiguration<EmployeeRole>
{
    public override void Configure(EntityTypeBuilder<EmployeeRole> builder)
    {
        base.Configure(builder);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(255);
    }
}
