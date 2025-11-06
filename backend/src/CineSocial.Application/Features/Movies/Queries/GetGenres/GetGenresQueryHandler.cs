using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Movies.Queries.GetGenres;

public class GetGenresQueryHandler : IRequestHandler<GetGenresQuery, Result<List<GenreDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetGenresQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<GenreDto>>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var genres = await _context.Genres
                .OrderBy(g => g.Name)
                .Select(g => new GenreDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .ToListAsync(cancellationToken);

            return Result<List<GenreDto>>.Success(genres);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Genre.RequestCancelled", "Request was cancelled");
        }
    }
}
