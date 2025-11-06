using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;

namespace CineSocial.Application.Features.Images.Queries.GetImage;

public record GetImageQuery(Guid ImageId) : IRequest<Result<Image>>;
