using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Commands.DeleteMovieList;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Commands.DeleteMovieList;

public class DeleteMovieListCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly DeleteMovieListCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _movieListId = Guid.NewGuid();

    public DeleteMovieListCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new DeleteMovieListCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_DeleteMovieList_When_UserIsOwnerAndNotWatchlist()
    {
        // Arrange
        var command = new DeleteMovieListCommand { MovieListId = _movieListId };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId, IsWatchlist = false };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<MovieList>().Delete(movieList), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_UserIsNotOwner()
    {
        // Arrange
        var command = new DeleteMovieListCommand { MovieListId = _movieListId };
        var movieList = new MovieList { Id = _movieListId, UserId = Guid.NewGuid() }; // Different user

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.Forbidden");
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_TryingToDeleteWatchlist()
    {
        // Arrange
        var command = new DeleteMovieListCommand { MovieListId = _movieListId };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId, IsWatchlist = true };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.CannotDeleteWatchlist");
    }
}
