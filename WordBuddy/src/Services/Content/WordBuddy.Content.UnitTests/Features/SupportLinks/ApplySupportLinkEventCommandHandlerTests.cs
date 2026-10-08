using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.SupportLinks;

public class ApplySupportLinkEventCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ISupportLinkProjectionRepository> _repository = new();
    private readonly Guid _linkId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();
    private readonly Guid _supporterId = Guid.NewGuid();
    private SupportLinkProjection? _stored;

    public ApplySupportLinkEventCommandHandlerTests()
    {
        _repository.Setup(r => r.GetTrackedAsync(_linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _stored is null
                ? Result.Failure<SupportLinkProjection>(Error.NotFound("SupportLinkProjection.NotFound", "none"))
                : Result.Success(_stored));
        _repository.Setup(r => r.AddAsync(It.IsAny<SupportLinkProjection>(), It.IsAny<CancellationToken>()))
            .Callback<SupportLinkProjection, CancellationToken>((p, _) => _stored = p)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private ApplySupportLinkEventCommandHandler CreateHandler() =>
        new(_repository.Object, new ApplySupportLinkEventCommandValidator(), Mock.Of<ILogger<ApplySupportLinkEventCommandHandler>>());

    private ApplySupportLinkEventCommand Event(bool isActive, DateTime at) => new(_linkId, _learnerId, _supporterId, isActive, at);

    [Fact]
    public async Task ApplySupportLinkEventCommandHandler_HandleAsync_FirstEventInsertsProjection()
    {
        (await CreateHandler().HandleAsync(Event(isActive: true, T0))).IsSuccess.Should().BeTrue();

        _stored!.IsActive.Should().BeTrue();
        _stored.LearnerId.Should().Be(_learnerId);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplySupportLinkEventCommandHandler_HandleAsync_ReplayChangesNothing()
    {
        ApplySupportLinkEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Event(isActive: true, T0));

        (await handler.HandleAsync(Event(isActive: true, T0))).IsSuccess.Should().BeTrue();

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _stored!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ApplySupportLinkEventCommandHandler_HandleAsync_OutOfOrderOlderEventIgnored()
    {
        ApplySupportLinkEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Event(isActive: false, T0.AddMinutes(5)));

        (await handler.HandleAsync(Event(isActive: true, T0))).IsSuccess.Should().BeTrue();

        _stored!.IsActive.Should().BeFalse();
        _stored.UpdatedAtUtc.Should().Be(T0.AddMinutes(5));
    }

    [Fact]
    public async Task ApplySupportLinkEventCommandHandler_HandleAsync_RevokeAfterActivateDeactivates()
    {
        ApplySupportLinkEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Event(isActive: true, T0));

        await handler.HandleAsync(Event(isActive: false, T0.AddDays(1)));

        _stored!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ApplySupportLinkEventCommandHandler_HandleAsync_EmptyIdsValidationError()
    {
        Result result = await CreateHandler().HandleAsync(new ApplySupportLinkEventCommand(Guid.Empty, _learnerId, _supporterId, true, T0));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
