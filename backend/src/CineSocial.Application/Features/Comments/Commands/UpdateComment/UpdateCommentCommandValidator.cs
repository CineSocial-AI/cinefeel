using FluentValidation;

namespace CineSocial.Application.Features.Comments.Commands.UpdateComment;

public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.CommentId)
            .NotEmpty().WithMessage("Comment ID is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MinimumLength(1).WithMessage("Comment must be at least 1 character")
            .MaximumLength(10000).WithMessage("Comment must not exceed 10,000 characters");
    }
}
