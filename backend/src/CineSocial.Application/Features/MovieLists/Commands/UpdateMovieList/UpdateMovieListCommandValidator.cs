using FluentValidation;

namespace CineSocial.Application.Features.MovieLists.Commands.UpdateMovieList;

public class UpdateMovieListCommandValidator : AbstractValidator<UpdateMovieListCommand>
{
    public UpdateMovieListCommandValidator()
    {
        RuleFor(x => x.MovieListId)
            .NotEmpty().WithMessage("Movie list ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required")
            .MinimumLength(1).WithMessage("List name must be at least 1 character")
            .MaximumLength(100).WithMessage("List name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
