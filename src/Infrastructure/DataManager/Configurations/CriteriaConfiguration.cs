using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class CriteriaConfiguration : BaseEntityConfiguration<Criteria>
{
    public override void Configure(EntityTypeBuilder<Criteria> builder)
    {
        base.Configure(builder);
        builder.HasIndex(w => w.Name).IsUnique();

        builder.Property(w => w.Name).IsRequired();
    }
}
