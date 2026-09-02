using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.EntraObjectId)
            .HasMaxLength(User.EntraObjectIdMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.EntraObjectId)
            .IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(User.DisplayNameMaxLength)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(x => x.LastLoginAt)
            .HasColumnType("datetime2");
    }
}