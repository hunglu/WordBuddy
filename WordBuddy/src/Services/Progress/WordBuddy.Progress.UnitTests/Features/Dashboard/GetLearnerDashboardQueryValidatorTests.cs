using FluentAssertions;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;

namespace WordBuddy.Progress.UnitTests.Features.Dashboard;

public class GetLearnerDashboardQueryValidatorTests
{
    private readonly GetLearnerDashboardQueryValidator _validator = new();

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    public void GetLearnerDashboardQueryValidator_Validate_AllowedDaysPass(int days)
    {
        _validator.Validate(new GetLearnerDashboardQuery(Guid.NewGuid(), null, days)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    [InlineData(14)]
    [InlineData(365)]
    public void GetLearnerDashboardQueryValidator_Validate_OtherDaysAreRejected(int days)
    {
        _validator.Validate(new GetLearnerDashboardQuery(Guid.NewGuid(), null, days)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetLearnerDashboardQueryValidator_Validate_EmptyLearnerIdIsRejected()
    {
        _validator.Validate(new GetLearnerDashboardQuery(Guid.Empty, null, 30)).IsValid.Should().BeFalse();
    }
}
