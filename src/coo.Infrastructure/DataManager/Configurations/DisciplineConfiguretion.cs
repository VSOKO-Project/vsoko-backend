using coo.Domain.Entities;
using coo.Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace coo.Infrastructure.DataManager.Configurations;

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