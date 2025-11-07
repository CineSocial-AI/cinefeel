using FluentValidation;

namespace CineSocial.Application.Features.Comments.Commands.AddReaction;

public class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
{
    public AddReactionCommandValidator()
    {
        RuleFor(x => x.CommentId)
            .NotEmpty().WithMessage("Comment ID is required");

        RuleFor(x => x.ReactionType)
            .IsInEnum().WithMessage("Invalid reaction type");
    }
}
