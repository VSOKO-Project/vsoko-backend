using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class AcademicPeriodConfiguration : BaseEntityConfiguration<AcademicPeriod>
{
    public override void Configure(EntityTypeBuilder<AcademicPeriod> builder)
    {
        base.Configure(builder);

        builder.Property(w => w.StartYear).IsRequired();
        builder.Property(w => w.Term).IsRequired();
        builder.Property(w => w.IsFeedbackOpen).IsRequired().HasDefaultValue(false);

        builder
            .HasIndex(w => new { w.StartYear, w.Term })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Сбор отзывов может быть открыт только у одного периода.
        builder
            .HasIndex(w => w.IsFeedbackOpen)
            .IsUnique()
            .HasFilter("\"IsFeedbackOpen\" = true AND \"IsDeleted\" = false");
    }
}
