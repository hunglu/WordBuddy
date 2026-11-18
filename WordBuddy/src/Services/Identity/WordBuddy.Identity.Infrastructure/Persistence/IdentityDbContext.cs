using Microsoft.EntityFrameworkCore;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Identity.Infrastructure.Persistence;

/// <summary>EF Core context for the Identity service database (<c>WordBuddyIdentity</c>).
/// Also holds the MassTransit outbox tables, so link events commit with the link change.
/// One shared model; the provider subclasses (<c>SqlServerIdentityDbContext</c>, <c>PostgresIdentityDbContext</c>) own their migrations.</summary>
public abstract class IdentityDbContext : DbContext
{
    /// <summary>Called by the provider subclasses with their own typed options.</summary>
    /// <param name="options">Provider-configured options.</param>
    protected IdentityDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<SupportLink> SupportLinks => Set<SupportLink>();
    public DbSet<SupportLinkInvitation> SupportLinkInvitations => Set<SupportLinkInvitation>();
    public DbSet<UnlinkRequest> UnlinkRequests => Set<UnlinkRequest>();
    public DbSet<SupportLinkAuditEntry> SupportLinkAuditEntries => Set<SupportLinkAuditEntry>();
    public DbSet<LearnerGroup> LearnerGroups => Set<LearnerGroup>();
    public DbSet<LearnerGroupMember> LearnerGroupMembers => Set<LearnerGroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.AddWordBuddyMessagingEntities();
    }
}
