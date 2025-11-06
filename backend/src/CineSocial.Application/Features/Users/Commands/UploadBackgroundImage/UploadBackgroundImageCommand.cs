using CineSocial.Application.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CineSocial.Application.Features.Users.Commands.UploadBackgroundImage;

public record UploadBackgroundImageCommand(IFormFile File) : IRequest<Result<UploadBackgroundImageResponse>>;

public record UploadBackgroundImageResponse(Guid ImageId, string Message);
