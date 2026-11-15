using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class ReportArchiveConfiguration : IEntityTypeConfiguration<ReportArchive>
{
    public void Configure(EntityTypeBuilder<ReportArchive> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PeriodTitle).HasMaxLength(200).IsRequired();
        builder.Property(r => r.FileName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Content).IsRequired();
        builder.HasIndex(r => new { r.CreatedAtUtc, r.Id });
    }
}
