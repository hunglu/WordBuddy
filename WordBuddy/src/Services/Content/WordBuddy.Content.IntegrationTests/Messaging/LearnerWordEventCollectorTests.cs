using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Messaging;
using WordBuddy.Shared.Contracts.Vocabulary;

namespace WordBuddy.Content.IntegrationTests.Messaging;

/// <summary>Pure unit tests of the change → event mapping (no database).</summary>
public sealed class LearnerWordEventCollectorTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LearnerWordEventCollector_Collect_AddedLinkGivesLearnerWordAdded()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: true);

        IReadOnlyList<object> events = LearnerWordEventCollector.Collect([(EntityState.Added, link)], Now);

        events.Should().ContainSingle().Which.Should().Be(
            new LearnerWordAdded(link.UserId, link.SenseId, link.UserId, link.AddedAtUtc));
    }

    [Fact]
    public void LearnerWordEventCollector_Collect_DeletedLinkGivesLearnerWordRemoved()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: false);

        IReadOnlyList<object> events = LearnerWordEventCollector.Collect([(EntityState.Deleted, link)], Now);

        events.Should().ContainSingle().Which.Should().Be(new LearnerWordRemoved(link.UserId, link.SenseId, Now));
    }

    [Fact]
    public void LearnerWordEventCollector_Collect_SystemOwnerLinkGivesNoEvent()
    {
        LearnerWord added = new(Guid.NewGuid(), SystemOwner.UserId, Guid.NewGuid(), isAuthor: true);
        LearnerWord deleted = new(Guid.NewGuid(), SystemOwner.UserId, Guid.NewGuid(), isAuthor: true);

        IReadOnlyList<object> events = LearnerWordEventCollector.Collect(
            [(EntityState.Added, added), (EntityState.Deleted, deleted)], Now);

        events.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EntityState.Modified)]
    [InlineData(EntityState.Unchanged)]
    public void LearnerWordEventCollector_Collect_OtherStatesGiveNoEvent(EntityState state)
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: true);

        IReadOnlyList<object> events = LearnerWordEventCollector.Collect([(state, link)], Now);

        events.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(LearnerWordAdded))]
    [InlineData(typeof(LearnerWordRemoved))]
    public void LearnerWordContracts_Payload_HasNoTextFields(Type contract)
    {
        contract.GetProperties().Select(p => p.PropertyType)
            .Should().OnlyContain(t => t == typeof(Guid) || t == typeof(DateTime));
    }
}
