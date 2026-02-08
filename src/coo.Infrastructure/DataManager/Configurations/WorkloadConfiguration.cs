using coo.Domain.Entities;
using coo.Infrastructure.DataManager.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace coo.Infrastructure.DataManager.Configurations;

public class WorkloadConfiguration : BaseEntityConfiguration<Workload>
{
    public override void Configure(EntityTypeBuilder<Workload> builder)
    {
        base.Configure(builder);

        builder.HasOne(w => w.TeacherRef)
        .WithMany(w => w.WorkloadsRefs)
        .HasForeignKey(w => w.TeacherId);

        builder.HasOne(w => w.DisciplineRef)
        .WithMany(w => w.WorkloadRefs)
        .HasForeignKey(w => w.DisciplineId);

        builder.HasOne(w => w.GroupRef)
        .WithMany(w => w.  WorkloadRefs)
        .HasForeignKey(w => w.GroupId);
    }
}