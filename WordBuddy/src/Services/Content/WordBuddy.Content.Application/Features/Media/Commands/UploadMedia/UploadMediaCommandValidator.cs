using FluentValidation;

namespace WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "audio/mpeg", "audio/wav", "video/mp4",
    ];

    public UploadMediaCommandValidator()
    {
        RuleFor(c => c.FileName).NotEmpty();
        RuleFor(c => c.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}.");
    }
}
