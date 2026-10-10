using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Dashboard;
using WordBuddy.Progress.Application.Features.Groups;
using WordBuddy.Progress.Application.Features.Groups.Queries.GetGroupDashboard;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.Groups;

public class GetGroupDashboardQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerGroupProjectionRepository> _groups = new();
    private readonly Mock<ISupportLinkProjectionRepository> _supportLinks = new();
    private readonly Mock<IDashboardReadRepository> _repository = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly List<LearnerGroupMemberProjection> _rows = [];
    private readonly HashSet<Guid> _linked = [];
    private IReadOnlyCollection<Guid>? _requestedIds;

    public GetGroupDashboardQueryHandlerTests()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        _groups.Setup(g => g.IsOwnerAsync(_groupId, _owner, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _groups.Setup(g => g.GetGroupAsync(_groupId, It.IsAny<CancellationToken>())).ReturnsAsync(() => _rows);
        _supportLinks.Setup(l => l.GetLearnersWithActiveLinkAsync(_owner, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) => (IReadOnlySet<Guid>)ids.Where(_linked.Contains).ToHashSet());

        _repository.Setup(r => r.GetReviewLogsForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Guid>, DateTime, DateTime, CancellationToken>((ids, _, _, _) => _requestedIds = ids)
            .ReturnsAsync(Result.Success<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardReviewRow>>>(new Dictionary<Guid, IReadOnlyList<DashboardReviewRow>>()));
        _repository.Setup(r => r.GetWordStatesForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardWordRow>>>(new Dictionary<Guid, IReadOnlyList<DashboardWordRow>>()));
        _repository.Setup(r => r.GetMembershipsForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardMembershipRow>>>(new Dictionary<Guid, IReadOnlyList<DashboardMembershipRow>>()));
        _repository.Setup(r => r.GetSessionIssuesForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardSessionRow>>>(new Dictionary<Guid, IReadOnlyList<DashboardSessionRow>>()));
    }

    private GetGroupDashboardQueryHandler CreateHandler() =>
        new(
            _groups.Object,
            _supportLinks.Object,
            _repository.Object,
            new DashboardCalculator(new DashboardOptions()),
            _cache.Object,
            new FixedTimeProvider(Now),
            new GetGroupDashboardQueryValidator(),
            Mock.Of<ILogger<GetGroupDashboardQueryHandler>>());

    private Guid GivenMember(bool active = true, bool linked = true)
    {
        Guid learner = Guid.NewGuid();
        _rows.Add(LearnerGroupMemberProjection.Create(_groupId, _owner, learner, active, Now));
        if (linked)
        {
            _linked.Add(learner);
        }

        return learner;
    }

    [Fact]
    public async Task GetGroupDashboardQueryHandler_HandleAsync_RemovedMemberAndInactiveLinkExcluded()
    {
        Guid adult = GivenMember();
        Guid child = GivenMember();
        GivenMember(active: false);
        GivenMember(linked: false);

        Result<GroupDashboardDto> result = await CreateHandler().HandleAsync(new GetGroupDashboardQuery(_groupId, _owner, null, 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Select(m => m.LearnerId).Should().BeEquivalentTo([adult, child]);
        result.Value.Totals.MemberCount.Should().Be(2);
        _requestedIds.Should().BeEquivalentTo([adult, child]);
    }

    [Fact]
    public async Task GetGroupDashboardQueryHandler_HandleAsync_OneBatchedReadPerMetric()
    {
        GivenMember();
        GivenMember();
        GivenMember();

        await CreateHandler().HandleAsync(new GetGroupDashboardQuery(_groupId, _owner, null, 7));

        _repository.Verify(r => r.GetReviewLogsForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetWordStatesForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetMembershipsForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetSessionIssuesForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGroupDashboardQueryHandler_HandleAsync_NonOwnerForbidden()
    {
        GivenMember();

        Result<GroupDashboardDto> result = await CreateHandler().HandleAsync(new GetGroupDashboardQuery(_groupId, Guid.NewGuid(), null, 30));

        result.Error.Should().Be(GroupDashboardErrors.Forbidden);
        _repository.Verify(r => r.GetReviewLogsForUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupDashboardQueryHandler_HandleAsync_InvalidDaysFailsValidation()
    {
        Result<GroupDashboardDto> result = await CreateHandler().HandleAsync(new GetGroupDashboardQuery(_groupId, _owner, null, 14));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetGroupDashboardQueryHandler_HandleAsync_StoresWithFiveMinuteAbsoluteExpiry()
    {
        GivenMember();
        DistributedCacheEntryOptions? stored = null;
        string key = GetGroupDashboardQueryHandler.CacheKey(_groupId, 30, TimeSpan.Zero);
        _cache.Setup(c => c.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, _, options, _) => stored = options)
            .Returns(Task.CompletedTask);

        await CreateHandler().HandleAsync(new GetGroupDashboardQuery(_groupId, _owner, null, 30));

        stored!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
        stored.SlidingExpiration.Should().BeNull();
    }

    [Fact]
    public void DashboardCalculator_CalculateGroup_MembersWithAndWithoutAnswersGetRowsAndMedian()
    {
        DashboardCalculator calculator = new(new DashboardOptions { MinSample = 1 });
        Guid active = Guid.NewGuid();
        Guid quiet = Guid.NewGuid();
        List<DashboardReviewRow> reviews =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(-2), VocabularySkill.Meaning, IsCorrect: true, ResponseMs: 2000, HintUsed: false, IsDue: true, AttemptNo: 1),
            new(Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-1), VocabularySkill.Meaning, IsCorrect: false, ResponseMs: 2000, HintUsed: false, IsDue: true, AttemptNo: 1),
        ];

        GroupDashboardDto dashboard = calculator.CalculateGroup(
            _groupId, 30, Now, TimeSpan.Zero, [active, quiet],
            new Dictionary<Guid, IReadOnlyList<DashboardReviewRow>> { [active] = reviews },
            new Dictionary<Guid, IReadOnlyList<DashboardWordRow>> { [active] = [new(Guid.NewGuid(), WordStatus.Leech, 4)] },
            new Dictionary<Guid, IReadOnlyList<DashboardMembershipRow>>(),
            new Dictionary<Guid, IReadOnlyList<DashboardSessionRow>>());

        dashboard.Members.Should().HaveCount(2);
        GroupMemberDashboardDto activeRow = dashboard.Members.Single(m => m.LearnerId == active);
        activeRow.ActiveDays.Should().Be(2);
        activeRow.Retention.Should().Be(50);
        activeRow.LeechCount.Should().Be(1);
        activeRow.LastActiveDate.Should().Be(new DateOnly(2026, 10, 9));
        GroupMemberDashboardDto quietRow = dashboard.Members.Single(m => m.LearnerId == quiet);
        quietRow.Retention.Should().BeNull();
        quietRow.LastActiveDate.Should().BeNull();
        dashboard.Totals.MedianRetention.Should().Be(50);
        dashboard.Totals.MembersActiveThisWeek.Should().Be(1);
    }
}
