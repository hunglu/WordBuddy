using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.AgeGroup).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsAdmin).IsRequired();

        builder.Property(u => u.Alias).HasMaxLength(User.AliasMaxLength);
        builder.HasIndex(u => u.Alias).IsUnique().HasFilter("[Alias] IS NOT NULL");
        builder.Property(u => u.AvatarId).HasMaxLength(30);
        builder.Ignore(u => u.PublicName);
    }
}
