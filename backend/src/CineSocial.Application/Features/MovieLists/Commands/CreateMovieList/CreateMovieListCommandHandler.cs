using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.CreateMovieList;

public class CreateMovieListCommandHandler : IRequestHandler<CreateMovieListCommand, Result<MovieListResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateMovieListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<MovieListResponse>> Handle(CreateMovieListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            var movieList = new MovieList
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Name = request.Name,
                Description = request.Description,
                IsPublic = request.IsPublic,
                IsWatchlist = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<MovieList>().AddAsync(movieList);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new MovieListResponse
            {
                MovieListId = movieList.Id,
                Name = movieList.Name,
                Description = movieList.Description,
                IsPublic = movieList.IsPublic,
                IsWatchlist = movieList.IsWatchlist,
                CreatedAt = movieList.CreatedAt
            });
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
