using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Search.Queries.SearchPeople;

public class SearchPeopleQueryHandler : IRequestHandler<SearchPeopleQuery, PagedResult<List<PersonSearchResultDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchPeopleQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<List<PersonSearchResultDto>>> Handle(SearchPeopleQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return PagedResult<List<PersonSearchResultDto>>.Failure(Error.Validation(
                    "Search.EmptyQuery",
                    "Search query cannot be empty"
                ));
            }

            var searchLower = request.Query.ToLower();
            var query = _unitOfWork.Repository<Person>()
                .Query()
                .Where(p => p.Name.ToLower().Contains(searchLower))
                .OrderByDescending(p => p.Popularity);

            var totalCount = await query.CountAsync(cancellationToken);

            var people = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PersonSearchResultDto
                {
                    Id = p.Id,
                    TmdbId = p.TmdbId,
                    Name = p.Name,
                    ProfilePath = p.ProfilePath,
                    Popularity = p.Popularity,
                    KnownForDepartment = p.KnownForDepartment,
                    MovieCount = p.MovieCasts.Count + p.MovieCrews.Count
                })
                .ToListAsync(cancellationToken);

            return PagedResult<List<PersonSearchResultDto>>.Success(
                people,
                request.Page,
                request.PageSize,
                totalCount
            );
        }
        catch (OperationCanceledException)
        {
            return PagedResult<List<PersonSearchResultDto>>.Failure(Error.Failure(
                "Search.RequestCancelled",
                "Request was cancelled"
            ));
        }
    }
}
