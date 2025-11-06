using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Rates.Commands.RateMovie;

public record RateMovieCommand(
    Guid MovieId,
    decimal Rating
) : IRequest<Result<RateResponse>>;

public record RateResponse
{
    public Guid RateId { get; init; }
    public Guid MovieId { get; init; }
    public Guid UserId { get; init; }
    public decimal Rating { get; init; }
    public bool IsNew { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
