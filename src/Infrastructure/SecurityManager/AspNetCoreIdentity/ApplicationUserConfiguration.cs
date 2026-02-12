using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SecurityManager.AspNetCoreIdentity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(60);

        builder.Property(w => w.Surname).IsRequired().HasMaxLength(70);

        builder.Property(w => w.Patronymic).HasMaxLength(70);

        builder.Property(w => w.Type).IsRequired();

        builder.Property(w => w.IsBlocked).IsRequired(false);

        builder.Property(w => w.IsDeleted).IsRequired(false);

        builder.Property(w => w.CreatedAt).IsRequired(false);

        builder.Property(w => w.CreatedById).IsRequired(false);

        builder.Property(w => w.UpdatedAt).IsRequired(false);

        builder.Property(w => w.UpdatedById).IsRequired(false);

        builder.HasIndex(w => w.Id);
        builder.HasIndex(w => w.UserName);
    }
}
