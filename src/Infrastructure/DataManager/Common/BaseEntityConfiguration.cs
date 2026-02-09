using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Common;

public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.CreatedAtUtc)
        .IsRequired(false)
        .HasDefaultValueSql("NOW()");
        builder.Property(w => w.CreatedById)
        .IsRequired(false);
        builder.Property(w => w.UpdatedAtUtc)
        .IsRequired(false);
        builder.Property(w => w.UpdatedById)
        .IsRequired(false);
    }
}