using FluentValidation;

namespace CineSocial.Application.Features.MovieLists.Commands.AddMovieToList;

public class AddMovieToListCommandValidator : AbstractValidator<AddMovieToListCommand>
{
    public AddMovieToListCommandValidator()
    {
        RuleFor(x => x.MovieListId)
            .NotEmpty().WithMessage("Movie list ID is required");

        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage("Movie ID is required");
    }
}
