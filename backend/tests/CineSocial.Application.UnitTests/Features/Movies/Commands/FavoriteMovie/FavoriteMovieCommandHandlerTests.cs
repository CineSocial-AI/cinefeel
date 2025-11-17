using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Commands.FavoriteMovie;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Movies.Commands.FavoriteMovie;

public class FavoriteMovieCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FavoriteMovieCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly int _movieId = 123;

    public FavoriteMovieCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new FavoriteMovieCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_FavoriteMovie_When_MovieExistsAndNotAlreadyFavorited()
    {
        // Arrange
        var command = new FavoriteMovieCommand { MovieId = _movieId };
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<MovieFavorite>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieFavorite, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((MovieFavorite)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<MovieFavorite>().AddAsync(It.IsAny<MovieFavorite>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_MovieDoesNotExist()
    {
        // Arrange
        var command = new FavoriteMovieCommand { MovieId = _movieId };
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Movie.NotFound");
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_MovieIsAlreadyFavorited()
    {
        // Arrange
        var command = new FavoriteMovieCommand { MovieId = _movieId };
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<MovieFavorite>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieFavorite, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MovieFavorite());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Movie.AlreadyFavorited");
    }
}
