using FluentValidation;

namespace WordBuddy.Application.Features.Media.Commands.UploadMedia;

internal sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "audio/mpeg",
        "audio/wav",
        "video/mp4",
    };

    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("'{PropertyValue}' is not a supported content type.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(50L * 1024 * 1024)
            .WithMessage("File size must not exceed 50 MB.");
    }
}
