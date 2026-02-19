using Domain.Entities;
using Infrastructure.DataManager.Common;
using Infrastructure.SecurityManager.AspNetCoreIdentity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataManager.Configurations;

public class RefreshConfiguration : BaseEntityConfiguration<Refresh>
{
    public override void Configure(EntityTypeBuilder<Refresh> builder)
    {
        base.Configure(builder);

        builder.Property(w => w.Token).IsRequired();

        builder.HasOne<ApplicationUser>().WithMany(w => w.Refreshes).HasForeignKey(w => w.UserId);

        builder.Property(w => w.ExpiresAt).IsRequired();
    }
}
