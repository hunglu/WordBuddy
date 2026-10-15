using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class ListChallengesQueryHandlerTests
{
    private readonly FakeChallengeRepository _repo = new();

    private ListChallengesQueryHandler Handler() =>
        new(_repo, new ListChallengesQueryValidator(), Mock.Of<ILogger<ListChallengesQueryHandler>>());

    private void SeedThreeAdultChallenges()
    {
        for (int i = 0; i < 3; i++)
        {
            _repo.Challenges.Add(NewChallenge(ChallengeAudience.Adult, status: ChallengeStatus.Running, startsAtUtc: Monday.AddDays(i)));
        }
    }

    [Fact]
    public async Task ListChallengesQueryHandler_HandleAsync_PagesAndKeepsTotal()
    {
        SeedThreeAdultChallenges();

        Result<PagedResult<ChallengeDto>> second = await Handler().HandleAsync(new ListChallengesQuery(Guid.NewGuid(), AgeGroup.Adult, 2, 2));

        second.IsSuccess.Should().BeTrue();
        second.Value.Items.Should().HaveCount(1);
        second.Value.TotalCount.Should().Be(3);
        second.Value.Page.Should().Be(2);
    }

    [Fact]
    public async Task ListChallengesQueryHandler_HandleAsync_ChildWithoutConsent_GetsEmptyPage()
    {
        _repo.Challenges.Add(NewChallenge(ChallengeAudience.Child, status: ChallengeStatus.Running));

        Result<PagedResult<ChallengeDto>> result = await Handler().HandleAsync(new ListChallengesQuery(Guid.NewGuid(), AgeGroup.Child, 1, 25));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ListChallengesQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<ChallengeDto>> result = await Handler().HandleAsync(new ListChallengesQuery(Guid.NewGuid(), AgeGroup.Adult, page, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
