using CineSocial.Application.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CineSocial.Application.Features.Users.Commands.UploadProfileImage;

public record UploadProfileImageCommand(IFormFile File) : IRequest<Result<UploadProfileImageResponse>>;

public record UploadProfileImageResponse(Guid ImageId, string Message);
