using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Rates.Commands.DeleteRate;

public class DeleteRateCommandHandler : IRequestHandler<DeleteRateCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteRateCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(DeleteRateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure(Error.Unauthorized(
                "Rate.Unauthorized",
                "User must be authenticated to delete rates"
            ));
        }

        // Find the rate
        var rate = await _unitOfWork.Repository<Rate>()
            .Query()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == request.MovieId, cancellationToken);

        if (rate == null)
        {
            return Result.Failure(Error.NotFound(
                "Rate.NotFound",
                "Rate not found for this movie"
            ));
        }

        _unitOfWork.Repository<Rate>().Delete(rate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
