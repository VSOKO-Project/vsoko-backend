using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataManager.Configurations;

public class CriteriaFeedbackConfiguration : BaseEntityConfiguration<CriteriaFeedback>
{
    public override void Configure(EntityTypeBuilder<CriteriaFeedback> builder)
    {
        base.Configure(builder);

        builder.ToTable(w =>
        w.HasCheckConstraint("CK_check_score", "\"CriteriaScore\"BETWEEN 1 AND 5"));

        builder.Property(w => w.CriteriaScore)
        .IsRequired()
        .HasDefaultValue(5);

        builder.HasOne(w => w.CriteriaRef)
        .WithMany(w => w.CriteriaFeedbackRefs)
        .HasForeignKey(w => w.CriteriaId);

        builder.HasOne(w => w.FeedbackRef)
        .WithMany(w => w.CriteriaFeedbackRefs)
        .HasForeignKey(w => w.FeedbackId);
    }
}