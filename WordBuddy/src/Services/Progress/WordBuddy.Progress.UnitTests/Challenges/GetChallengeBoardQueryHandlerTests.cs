using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Challenges;
using WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class GetChallengeBoardQueryHandlerTests
{
    private readonly FakeChallengeRepository _repo = new();
    private readonly FakeChallengeProfileRepository _profiles = new();
    private readonly FakeDistributedCache _cache = new();

    private GetChallengeBoardQueryHandler Handler() =>
        new(_repo, _profiles, _cache, new GetChallengeBoardQueryValidator(), Mock.Of<ILogger<GetChallengeBoardQueryHandler>>());

    /// <summary>Eight participants; participant i has rank i and score (9 - i) * 10.</summary>
    private (Challenge Challenge, List<Guid> Users) EightLearners(ChallengeAudience audience)
    {
        Challenge challenge = NewChallenge(audience, status: ChallengeStatus.Running);
        _repo.Challenges.Add(challenge);
        AgeGroup age = audience == ChallengeAudience.Child ? AgeGroup.Child : AgeGroup.Adult;
        List<Guid> users = [];
        for (int i = 1; i <= 8; i++)
        {
            Guid user = Guid.NewGuid();
            users.Add(user);
            _profiles.Profiles[user] = Profile(user, age, alias: i == 3 ? null : $"Alias{i}", displayName: $"Real Name {i}");
            ChallengeParticipant participant = ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, user, Monday.AddMinutes(i));
            participant.ApplyProjection((9 - i) * 10, i, targetReached: false);
            _repo.Participants.Add(participant);
        }

        return (challenge, users);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_ChildSeesTopFiveAndOwnRowOnly()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Child);
        _repo.Consents.Add(ChallengeConsent.Create(users[6], Guid.NewGuid(), Monday));

        Result<ChallengeBoardDto> result = await Handler().HandleAsync(new GetChallengeBoardQuery(users[6], challenge.Id, AgeGroup.Child));

        ChallengeBoardDto board = result.Value;
        board.Entries.Items.Select(e => e.Rank).Should().Equal(1, 2, 3, 4, 5, 7);
        board.MyRank.Should().Be(7);
        board.MyScore.Should().Be(20);
        board.ParticipantCount.Should().Be(8);
        board.Entries.Items.Single(e => e.IsMe).Score.Should().Be(20);
        board.Entries.Items.Where(e => !e.IsMe).Should().OnlyContain(e => e.Score == null && e.TargetReached == null);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_ChildOnTheTopFiveGetsNoDuplicateRow()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Child);
        _repo.Consents.Add(ChallengeConsent.Create(users[1], Guid.NewGuid(), Monday));

        ChallengeBoardDto board = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[1], challenge.Id, AgeGroup.Child))).Value;

        board.Entries.Items.Should().HaveCount(5);
        board.Entries.Items.Count(e => e.IsMe).Should().Be(1);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_ChildBoardHoldsAliasAndAvatarOnlyNoDisplayNameNoUserIds()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Child);
        _repo.Consents.Add(ChallengeConsent.Create(users[0], Guid.NewGuid(), Monday));

        ChallengeBoardDto board = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[0], challenge.Id, AgeGroup.Child))).Value;

        board.Entries.Items.Select(e => e.Name).Should().Equal("Alias1", "Alias2", ChallengeProfile.ChildFallbackName, "Alias4", "Alias5");
        board.Entries.Items.Should().OnlyContain(e => e.AvatarId == "owl");
        string json = JsonSerializer.Serialize(board);
        json.Should().NotContain("Real Name");
        foreach (Guid user in users)
        {
            json.Should().NotContain(user.ToString());
        }

        typeof(ChallengeBoardEntryDto).GetProperties().Should().NotContain(p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?));
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_AdultSeesTheFullRankingWithScores()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Adult);

        ChallengeBoardDto board = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[6], challenge.Id, AgeGroup.Adult))).Value;

        board.Entries.Items.Should().HaveCount(8);
        board.Entries.Items.Select(e => e.Rank).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        board.Entries.Items.Should().OnlyContain(e => e.Score != null);
        board.Entries.Items[2].Name.Should().Be("Real Name 3", "an adult without an alias shows the display name");
        board.Entries.Items.Single(e => e.IsMe).Rank.Should().Be(7);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_ChildWithoutConsentIsRejected()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Child);

        Result<ChallengeBoardDto> result = await Handler().HandleAsync(new GetChallengeBoardQuery(users[0], challenge.Id, AgeGroup.Child));

        result.Error.Should().Be(ChallengeErrors.NoConsent);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_OtherAudienceIsRejected()
    {
        (Challenge challenge, _) = EightLearners(ChallengeAudience.Child);

        Result<ChallengeBoardDto> asAdult = await Handler().HandleAsync(new GetChallengeBoardQuery(Guid.NewGuid(), challenge.Id, AgeGroup.Adult));

        asAdult.Error.Should().Be(ChallengeErrors.AudienceMismatch);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_UnknownChallengeIsNotFound()
    {
        Result<ChallengeBoardDto> result = await Handler().HandleAsync(new GetChallengeBoardQuery(Guid.NewGuid(), Guid.NewGuid(), AgeGroup.Adult));

        result.Error.Should().Be(ChallengeErrors.NotFound);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_SecondCallComesFromCacheAndIsStillFilteredPerCaller()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Child);
        _repo.Consents.Add(ChallengeConsent.Create(users[0], Guid.NewGuid(), Monday));
        _repo.Consents.Add(ChallengeConsent.Create(users[7], Guid.NewGuid(), Monday));

        ChallengeBoardDto first = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[0], challenge.Id, AgeGroup.Child))).Value;
        ChallengeBoardDto second = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[7], challenge.Id, AgeGroup.Child))).Value;

        _repo.ReadBoardCount.Should().Be(1);
        _cache.Items.Should().ContainKey(ChallengeCache.BoardKey(challenge.Id));
        first.MyRank.Should().Be(1);
        second.MyRank.Should().Be(8);
        second.Entries.Items.Last().IsMe.Should().BeTrue();
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_AdultPageTwoIsTheRightSlice()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Adult);

        ChallengeBoardDto board = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[0], challenge.Id, AgeGroup.Adult, 2, 3))).Value;

        board.Entries.Items.Select(e => e.Rank).Should().Equal(4, 5, 6);
        board.Entries.Page.Should().Be(2);
        board.Entries.PageSize.Should().Be(3);
        board.Entries.TotalCount.Should().Be(8);
        board.ParticipantCount.Should().Be(8);
        board.MyRank.Should().Be(1, "my rank does not depend on the page");
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_AdultPastTheEndIsEmptyWithTotal()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Adult);

        ChallengeBoardDto board = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[0], challenge.Id, AgeGroup.Adult, 9, 25))).Value;

        board.Entries.Items.Should().BeEmpty();
        board.Entries.TotalCount.Should().Be(8);
    }

    [Fact]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_ChildWithPageTwoStillGetsTopFiveAndOwnRowOnly()
    {
        (Challenge challenge, List<Guid> users) = EightLearners(ChallengeAudience.Child);
        _repo.Consents.Add(ChallengeConsent.Create(users[6], Guid.NewGuid(), Monday));

        ChallengeBoardDto board = (await Handler().HandleAsync(new GetChallengeBoardQuery(users[6], challenge.Id, AgeGroup.Child, 2, 2))).Value;

        board.Entries.Items.Select(e => e.Rank).Should().Equal(1, 2, 3, 4, 5, 7);
        board.Entries.Page.Should().Be(1);
        board.Entries.TotalCount.Should().Be(6);
        board.Entries.Items.Where(e => !e.IsMe).Should().OnlyContain(e => e.Score == null && e.TargetReached == null);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetChallengeBoardQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<ChallengeBoardDto> result = await Handler().HandleAsync(new GetChallengeBoardQuery(Guid.NewGuid(), Guid.NewGuid(), AgeGroup.Adult, page, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
