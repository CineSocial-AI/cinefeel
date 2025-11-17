using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Services;
using CineSocial.Application.Features.Images.Queries.GetImage;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using CineSocial.Application.Common.Results;

namespace CineSocial.Application.UnitTests.Features.Images.Queries.GetImage;

public class GetImageQueryHandlerTests
{
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly GetImageQueryHandler _handler;

    public GetImageQueryHandlerTests()
    {
        _imageServiceMock = new Mock<IImageService>();
        _handler = new GetImageQueryHandler(_imageServiceMock.Object);
    }

    [Fact]
    public async Task Should_ReturnImage_When_ImageExists()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var query = new GetImageQuery { ImageId = imageId };
        var testImage = new Image { Id = imageId, CloudUrl = "http://example.com/image.jpg" };

        _imageServiceMock.Setup(s => s.GetImageByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(testImage));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(testImage);
        _imageServiceMock.Verify(s => s.GetImageByIdAsync(imageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnError_When_ImageServiceFails()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var query = new GetImageQuery { ImageId = imageId };
        var error = Error.NotFound("Image.NotFound", "Image not found");

        _imageServiceMock.Setup(s => s.GetImageByIdAsync(imageId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<Image>(error));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Image.NotFound");
    }
}
