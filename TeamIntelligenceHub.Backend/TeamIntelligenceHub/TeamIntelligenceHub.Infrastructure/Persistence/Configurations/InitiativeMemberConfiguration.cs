using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class InitiativeMemberConfiguration
    : IEntityTypeConfiguration<InitiativeMember>
{
    public void Configure(EntityTypeBuilder<InitiativeMember> builder)
    {
        builder.ToTable("InitiativeMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InitiativeId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasMaxLength(InitiativeMember.RoleMaxLength)
            .IsRequired();

        builder.Property(x => x.ResponsibilityArea)
            .HasMaxLength(InitiativeMember.ResponsibilityAreaMaxLength);

        builder.Property(x => x.Allocation)
            .HasColumnType("decimal(5,2)");

        // datetime2 stores no offset, so EF hands this back as Unspecified and it
        // serialises without a "Z" — the browser would then read UTC as local time.
        builder.Property(x => x.JoinedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        // A person appears on an Initiative once. Without this the same user could be
        // added repeatedly, and no application check survives concurrent requests.
        builder.HasIndex(x => new { x.InitiativeId, x.UserId })
            .IsUnique();

        // Membership belongs to the Initiative, so it goes when the Initiative goes.
        builder.HasOne(x => x.Initiative)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.InitiativeId)
            .OnDelete(DeleteBehavior.Cascade);

        // A person is not owned by an Initiative, so deleting one must not remove them.
        // Restrict also matches how Owner and ExecutiveSponsor behave.
        builder.HasOne(x => x.User)
            .WithMany(x => x.InitiativeMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
