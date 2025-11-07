using FluentValidation;

namespace CineSocial.Application.Features.Users.Commands.UploadBackgroundImage;

public class UploadBackgroundImageCommandValidator : AbstractValidator<UploadBackgroundImageCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB (background images can be larger)

    public UploadBackgroundImageCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty")
            .Must(file => file.Length <= MaxFileSize).WithMessage($"File size must not exceed {MaxFileSize / 1024 / 1024}MB")
            .Must(file =>
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                return AllowedExtensions.Contains(extension);
            }).WithMessage($"Invalid file type. Allowed types: {string.Join(", ", AllowedExtensions)}");
    }
}
