using FluentValidation;

namespace CineSocial.Application.Features.Rates.Commands.RateMovie;

public class RateMovieCommandValidator : AbstractValidator<RateMovieCommand>
{
    public RateMovieCommandValidator()
    {
        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage("Movie ID is required");

        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 10).WithMessage("Rating must be between 0 and 10")
            .PrecisionScale(3, 1, true).WithMessage("Rating must have at most 1 decimal place");
    }
}
