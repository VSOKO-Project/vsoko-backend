using coo.Domain.Entities;
using coo.Infrastructure.DataManager.Common;
using coo.Infrastructure.SecurityManager;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using coo.Infrastructure.SecurityManager.AspNetCoreIdentity;


namespace coo.Infrastructure.DataManager.Configurations;

public class StudentConfiguration : BaseEntityConfiguration<Student>
{
    public override void Configure(EntityTypeBuilder<Student> builder)
    {
        base.Configure(builder);

        builder.HasOne<ApplicationUser>()
        .WithOne(w => w.StudentRef)
        .HasForeignKey<Student>(w => w.Id);

        builder.HasOne(w => w.GroupRef)
        .WithMany(w => w.StudentRefs)
        .HasForeignKey(w => w.GroupId);
    }
}