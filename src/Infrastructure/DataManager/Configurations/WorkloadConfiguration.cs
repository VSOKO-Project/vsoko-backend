using Domain.Entities;
using Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class WorkloadConfiguration : BaseEntityConfiguration<Workload>
{
    public override void Configure(EntityTypeBuilder<Workload> builder)
    {
        base.Configure(builder);

        builder
            .HasOne(w => w.TeacherRef)
            .WithMany(w => w.WorkloadsRefs)
            .HasForeignKey(w => w.TeacherId);

        builder
            .HasOne(w => w.DisciplineRef)
            .WithMany(w => w.WorkloadRefs)
            .HasForeignKey(w => w.DisciplineId);

        builder.HasOne(w => w.GroupRef).WithMany(w => w.WorkloadRefs).HasForeignKey(w => w.GroupId);

        builder
            .HasOne(w => w.PeriodRef)
            .WithMany(w => w.WorkloadRefs)
            .HasForeignKey(w => w.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(w => new { w.TeacherId, w.DisciplineId, w.GroupId, w.PeriodId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
