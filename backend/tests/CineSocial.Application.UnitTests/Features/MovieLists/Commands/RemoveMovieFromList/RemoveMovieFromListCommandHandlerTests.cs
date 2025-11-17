using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Commands.RemoveMovieFromList;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Commands.RemoveMovieFromList;

public class RemoveMovieFromListCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly RemoveMovieFromListCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _movieListId = Guid.NewGuid();
    private readonly int _movieId = 1234;

    public RemoveMovieFromListCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new RemoveMovieFromListCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_RemoveMovieFromList_When_UserIsOwnerAndMovieExists()
    {
        // Arrange
        var command = new RemoveMovieFromListCommand { MovieListId = _movieListId, MovieId = _movieId };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId };
        var movieListItem = new MovieListItem { MovieListId = _movieListId, MovieId = _movieId };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);
        _unitOfWorkMock.Setup(u => u.Repository<MovieListItem>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieListItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieListItem);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<MovieListItem>().HardDelete(movieListItem), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_MovieIsNotInList()
    {
        // Arrange
        var command = new RemoveMovieFromListCommand { MovieListId = _movieListId, MovieId = _movieId };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);
        _unitOfWorkMock.Setup(u => u.Repository<MovieListItem>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieListItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((MovieListItem)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.MovieNotInList");
    }
}
