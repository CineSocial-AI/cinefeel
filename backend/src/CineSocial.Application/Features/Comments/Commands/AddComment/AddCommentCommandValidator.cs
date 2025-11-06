using FluentValidation;

namespace CineSocial.Application.Features.Comments.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage("Movie ID is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MinimumLength(1).WithMessage("Comment must be at least 1 character")
            .MaximumLength(10000).WithMessage("Comment must not exceed 10,000 characters");
    }
}
