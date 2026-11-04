using Grpc.Core;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Grpc;

/// <summary>Maps a failed <see cref="Error"/> to the matching gRPC <see cref="RpcException"/>.
/// The gRPC counterpart of <c>ToProblemResult</c> for controllers.</summary>
public static class RpcStatusMapper
{
    /// <summary>Builds the exception: <c>Validation</c> to <c>InvalidArgument</c>, <c>NotFound</c> to
    /// <c>NotFound</c>, anything else to <c>Internal</c>.</summary>
    public static RpcException ToRpcException(this Error error)
    {
        StatusCode code = error.Type switch
        {
            ErrorType.Validation => StatusCode.InvalidArgument,
            ErrorType.NotFound => StatusCode.NotFound,
            _ => StatusCode.Internal,
        };

        return new RpcException(new Status(code, error.Code));
    }
}
