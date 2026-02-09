using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class TeacherConfiguration : BaseEntityConfiguration<Teacher>
{
    public override void Configure(EntityTypeBuilder<Teacher> builder)
    {
        base.Configure(builder);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(255);
        builder.Property(w => w.Surname).IsRequired().HasMaxLength(255);
        builder.Property(w => w.Patronymic).IsRequired().HasMaxLength(255);
    }
}