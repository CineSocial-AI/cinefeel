using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Search.Queries.SearchAll;

public class SearchAllQueryHandler : IRequestHandler<SearchAllQuery, Result<SearchAllResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchAllQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SearchAllResultDto>> Handle(SearchAllQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Error.Validation("Search.EmptyQuery", "Search query cannot be empty");
            }

            var searchLower = request.Query.ToLower();

            // Search movies
            var movies = await _unitOfWork.Repository<MovieEntity>()
                .Query()
                .Where(m => m.Title.ToLower().Contains(searchLower) ||
                           (m.OriginalTitle != null && m.OriginalTitle.ToLower().Contains(searchLower)))
                .OrderByDescending(m => m.Popularity)
                .Take(10)
                .Select(m => new MovieSearchItemDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    PosterPath = m.PosterPath,
                    VoteAverage = m.VoteAverage,
                    ReleaseDate = m.ReleaseDate
                })
                .ToListAsync(cancellationToken);

            // Search people
            var people = await _unitOfWork.Repository<Person>()
                .Query()
                .Where(p => p.Name.ToLower().Contains(searchLower))
                .OrderByDescending(p => p.Popularity)
                .Take(10)
                .Select(p => new PersonSearchItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProfilePath = p.ProfilePath,
                    KnownForDepartment = p.KnownForDepartment,
                    Popularity = p.Popularity
                })
                .ToListAsync(cancellationToken);

            var result = new SearchAllResultDto
            {
                Movies = movies,
                People = people
            };

            return Result<SearchAllResultDto>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Search.RequestCancelled", "Request was cancelled");
        }
    }
}
