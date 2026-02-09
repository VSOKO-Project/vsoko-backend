using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class DisciplineConfiguration : BaseEntityConfiguration<Discipline>
{
    public override void Configure(EntityTypeBuilder<Discipline> builder)
    {
        base.Configure(builder);

        builder.Property(w => w.Name)
        .IsRequired()
        .HasMaxLength(100);
    }
}