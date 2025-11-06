using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.People.Queries.GetPersonDetail;

public class GetPersonDetailQueryHandler : IRequestHandler<GetPersonDetailQuery, Result<PersonDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPersonDetailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PersonDetailDto>> Handle(GetPersonDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var person = await _unitOfWork.Repository<Person>()
                .Query()
                .Include(p => p.MovieCasts)
                    .ThenInclude(mc => mc.Movie)
                .Include(p => p.MovieCrews)
                    .ThenInclude(mc => mc.Movie)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (person == null)
            {
                return Error.NotFound("Person.NotFound", "Person not found");
            }

            var castCredits = person.MovieCasts
                .OrderByDescending(mc => mc.Movie.ReleaseDate)
                .Select(mc => new PersonMovieCreditDto
                {
                    MovieId = mc.MovieId,
                    Title = mc.Movie.Title,
                    PosterPath = mc.Movie.PosterPath,
                    ReleaseDate = mc.Movie.ReleaseDate,
                    VoteAverage = mc.Movie.VoteAverage,
                    Character = mc.Character
                })
                .ToList();

            var crewCredits = person.MovieCrews
                .OrderByDescending(mc => mc.Movie.ReleaseDate)
                .Select(mc => new PersonMovieCreditDto
                {
                    MovieId = mc.MovieId,
                    Title = mc.Movie.Title,
                    PosterPath = mc.Movie.PosterPath,
                    ReleaseDate = mc.Movie.ReleaseDate,
                    VoteAverage = mc.Movie.VoteAverage,
                    Job = mc.Job,
                    Department = mc.Department
                })
                .ToList();

            var result = new PersonDetailDto
            {
                Id = person.Id,
                TmdbId = person.TmdbId,
                Name = person.Name,
                Biography = person.Biography,
                Birthday = person.Birthday,
                Deathday = person.Deathday,
                PlaceOfBirth = person.PlaceOfBirth,
                ProfilePath = person.ProfilePath,
                Popularity = person.Popularity,
                Gender = person.Gender,
                KnownForDepartment = person.KnownForDepartment,
                ImdbId = person.ImdbId,
                CastCredits = castCredits,
                CrewCredits = crewCredits
            };

            return Result<PersonDetailDto>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Person.RequestCancelled", "Request was cancelled");
        }
    }
}
