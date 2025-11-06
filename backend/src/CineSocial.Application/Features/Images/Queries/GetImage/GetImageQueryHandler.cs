using CineSocial.Application.Common.Results;
using CineSocial.Application.Services;
using CineSocial.Domain.Entities.User;
using MediatR;

namespace CineSocial.Application.Features.Images.Queries.GetImage;

public class GetImageQueryHandler : IRequestHandler<GetImageQuery, Result<Image>>
{
    private readonly IImageService _imageService;

    public GetImageQueryHandler(IImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task<Result<Image>> Handle(GetImageQuery request, CancellationToken cancellationToken)
    {
        return await _imageService.GetImageByIdAsync(request.ImageId, cancellationToken);
    }
}
