using Microsoft.AspNetCore.Mvc;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Extensions;

/// <summary>Maps a failed <see cref="Result"/>/<see cref="Result{T}"/> to the matching RFC 7807 <see cref="ProblemDetails"/> response.</summary>
internal static class ResultExtensions
{
    // The shared Kernel has no Forbidden error type; codes ending in this suffix map to 403.
    private const string ForbiddenSuffix = "Forbidden";

    public static IActionResult ToProblemResult(this Result result, ControllerBase controller)
    {
        int statusCode = result.Error.Code.EndsWith(ForbiddenSuffix, StringComparison.Ordinal)
            ? StatusCodes.Status403Forbidden
            : result.Error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest,
            };

        return controller.Problem(
            title: result.Error.Code,
            detail: result.Error.Description,
            statusCode: statusCode);
    }
}
