using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Commands.UnfavoriteMovie;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Movies.Commands.UnfavoriteMovie;

public class UnfavoriteMovieCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UnfavoriteMovieCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly int _movieId = 123;

    public UnfavoriteMovieCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new UnfavoriteMovieCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_UnfavoriteMovie_When_MovieIsFavorited()
    {
        // Arrange
        var command = new UnfavoriteMovieCommand { MovieId = _movieId };
        var existingFavorite = new MovieFavorite { UserId = _currentUserId, MovieId = _movieId };
        _unitOfWorkMock.Setup(u => u.Repository<MovieFavorite>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieFavorite, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingFavorite);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<MovieFavorite>().HardDelete(existingFavorite), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_MovieIsNotFavorited()
    {
        // Arrange
        var command = new UnfavoriteMovieCommand { MovieId = _movieId };
        _unitOfWorkMock.Setup(u => u.Repository<MovieFavorite>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieFavorite, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((MovieFavorite)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Movie.NotFavorited");
    }
}
