using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class ResponseTimeEvaluatorTests
{
    private static readonly DateTime Issued = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private static readonly ResponseTimeEvaluator Evaluator = new(new VocabularyGradingOptions());

    [Fact]
    public void VocabularyGradingOptions_ToleranceMs_DefaultsTo3000()
    {
        new VocabularyGradingOptions().ToleranceMs.Should().Be(3000);
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_AcceptsPlausibleClientValue()
    {
        ResponseTiming timing = Evaluator.Evaluate(4200, Issued, Issued.AddMilliseconds(5000));

        timing.Should().Be(new ResponseTiming(UsedMs: 4200, ClientMs: 4200, ServerMs: 5000, Adjusted: false));
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_RejectsForgedZeroWhenServerTimeIsLong()
    {
        ResponseTiming timing = Evaluator.Evaluate(0, Issued, Issued.AddMilliseconds(8000));

        timing.Adjusted.Should().BeTrue();
        timing.UsedMs.Should().Be(5000);
        timing.ClientMs.Should().Be(0);
        timing.ServerMs.Should().Be(8000);
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_AcceptsZeroWhenServerTimeIsWithinTolerance()
    {
        ResponseTiming timing = Evaluator.Evaluate(0, Issued, Issued.AddMilliseconds(1500));

        timing.Adjusted.Should().BeFalse();
        timing.UsedMs.Should().Be(0);
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_RejectsClientValueAboveServerTime()
    {
        ResponseTiming timing = Evaluator.Evaluate(9000, Issued, Issued.AddMilliseconds(6000));

        timing.Adjusted.Should().BeTrue();
        timing.UsedMs.Should().Be(3000);
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_RejectsNegativeClientValue()
    {
        ResponseTiming timing = Evaluator.Evaluate(-5, Issued, Issued.AddMilliseconds(4000));

        timing.Adjusted.Should().BeTrue();
        timing.UsedMs.Should().Be(1000);
    }

    [Theory]
    [InlineData(2000, false)] // gap exactly at the tolerance: accepted
    [InlineData(1999, true)] // gap one ms over: rejected
    public void ResponseTimeEvaluator_Evaluate_BoundaryAtTolerance(int clientMs, bool adjusted)
    {
        ResponseTiming timing = Evaluator.Evaluate(clientMs, Issued, Issued.AddMilliseconds(5000));

        timing.Adjusted.Should().Be(adjusted);
        timing.UsedMs.Should().Be(adjusted ? 2000 : clientMs);
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_FloorsAdjustedValueAtZero()
    {
        ResponseTiming timing = Evaluator.Evaluate(2600, Issued, Issued.AddMilliseconds(2500));

        timing.Adjusted.Should().BeTrue();
        timing.UsedMs.Should().Be(0);
    }

    [Fact]
    public void ResponseTimeEvaluator_Evaluate_UsesConfiguredTolerance()
    {
        ResponseTimeEvaluator strict = new(new VocabularyGradingOptions { ToleranceMs = 500 });

        ResponseTiming timing = strict.Evaluate(1000, Issued, Issued.AddMilliseconds(2000));

        timing.Adjusted.Should().BeTrue();
        timing.UsedMs.Should().Be(1500);
    }
}
