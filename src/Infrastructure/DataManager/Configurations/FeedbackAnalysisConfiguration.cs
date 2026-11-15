using Domain.Entities;
using Infrastructure.AIManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class FeedbackAnalysisConfiguration : IEntityTypeConfiguration<FeedbackAnalysis>
{
    public void Configure(EntityTypeBuilder<FeedbackAnalysis> builder)
    {
        builder.HasKey(x => x.FeedbackId);
        builder.HasOne(x => x.Feedback).WithOne().HasForeignKey<FeedbackAnalysis>(x => x.FeedbackId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.FindingsJson).HasColumnType("jsonb");
        builder.Property(x => x.Status).HasMaxLength(24);
        builder.Property(x => x.Version).HasMaxLength(64);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.HasIndex(x => x.RetryAfterUtc);
    }
}

public class FeedbackCategoryConfiguration : IEntityTypeConfiguration<FeedbackCategory>
{
    public void Configure(EntityTypeBuilder<FeedbackCategory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.NormalizedName).HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(600);
        builder.HasIndex(x => x.NormalizedName).IsUnique();
        builder.HasData(FeedbackCategoryCatalog.Defaults);
    }
}

public class FeedbackSignalReviewConfiguration : IEntityTypeConfiguration<FeedbackSignalReview>
{
    public void Configure(EntityTypeBuilder<FeedbackSignalReview> builder)
    {
        builder.HasKey(x => new { x.FeedbackId, x.SignalKey });
        builder.Property(x => x.SignalKey).HasMaxLength(64);
        builder.Property(x => x.Status).HasMaxLength(24);
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.HasOne(x => x.Feedback).WithMany().HasForeignKey(x => x.FeedbackId).OnDelete(DeleteBehavior.Cascade);
    }
}
