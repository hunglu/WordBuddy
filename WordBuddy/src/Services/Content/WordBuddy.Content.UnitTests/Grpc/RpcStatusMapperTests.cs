using FluentAssertions;
using Grpc.Core;
using WordBuddy.Content.Api.Grpc;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Grpc;

public sealed class RpcStatusMapperTests
{
    [Fact]
    public void ToRpcException_ValidationError_ReturnsInvalidArgument()
    {
        RpcException exception = Error.Validation("Sense.Invalid", "bad").ToRpcException();

        exception.StatusCode.Should().Be(StatusCode.InvalidArgument);
        exception.Status.Detail.Should().Be("Sense.Invalid");
    }

    [Fact]
    public void ToRpcException_NotFoundError_ReturnsNotFound()
    {
        Error.NotFound("Sense.NotFound", "missing").ToRpcException().StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public void ToRpcException_OtherError_ReturnsInternal()
    {
        Error.Failure("Sense.Failed", "boom").ToRpcException().StatusCode.Should().Be(StatusCode.Internal);
        Error.Conflict("Sense.Conflict", "dup").ToRpcException().StatusCode.Should().Be(StatusCode.Internal);
    }
}
