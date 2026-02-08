using coo.Domain.Entities;
using coo.Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace coo.Infrastructure.DataManager.Configurations;

public class StudentGroupConfiguration : BaseEntityConfiguration<StudentGroup>
{
    public override void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        base.Configure(builder);

        builder.HasIndex(w => w.Name).IsUnique();

        builder.Property(w => w.Name)
        .IsRequired()
        .HasMaxLength(10);

        builder.Property(w => w.Semester)
        .IsRequired();
    }
}