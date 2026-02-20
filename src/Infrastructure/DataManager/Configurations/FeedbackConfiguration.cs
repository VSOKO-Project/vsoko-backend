using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class FeedbackConfiguration : BaseEntityConfiguration<Feedback>
{
    public override void Configure(EntityTypeBuilder<Feedback> builder)
    {
        base.Configure(builder);

        builder.Property(w => w.Comment).HasMaxLength(510);

        builder
            .HasOne(w => w.StudentRef)
            .WithMany(w => w.FeedbackRefs)
            .HasForeignKey(w => w.StudentId);

        builder
            .HasOne(w => w.WorkloadRef)
            .WithMany(w => w.FeedbackRefs)
            .HasForeignKey(w => w.WorkloadId);

        builder
            .HasIndex(f => new { f.StudentId, f.WorkloadId })
            .IsUnique();
    }
}
