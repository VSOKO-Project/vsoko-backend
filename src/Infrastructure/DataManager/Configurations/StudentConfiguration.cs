using Domain.Entities;
using Infrastructure.DataManager.Common;
using Infrastructure.SecurityManager;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class StudentConfiguration : BaseEntityConfiguration<Student>
{
    public override void Configure(EntityTypeBuilder<Student> builder)
    {
        base.Configure(builder);

        builder
            .HasOne<ApplicationUser>()
            .WithOne(w => w.StudentRef)
            .HasForeignKey<Student>(w => w.Id);

        builder.HasOne(w => w.GroupRef).WithMany(w => w.StudentRefs).HasForeignKey(w => w.GroupId);
    }
}
