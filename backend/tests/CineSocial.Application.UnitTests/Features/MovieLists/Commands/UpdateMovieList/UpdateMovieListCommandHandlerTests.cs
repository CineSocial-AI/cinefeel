using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Commands.UpdateMovieList;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Commands.UpdateMovieList;

public class UpdateMovieListCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UpdateMovieListCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _movieListId = Guid.NewGuid();

    public UpdateMovieListCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new UpdateMovieListCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_UpdateMovieList_When_UserIsOwner()
    {
        // Arrange
        var command = new UpdateMovieListCommand { MovieListId = _movieListId, Name = "New Name", Description = "New Desc", IsPublic = true };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId, Name = "Old Name" };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        movieList.Name.Should().Be("New Name");
        movieList.Description.Should().Be("New Desc");
        _unitOfWorkMock.Verify(u => u.Repository<MovieList>().Update(movieList), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_RenamingWatchlist()
    {
        // Arrange
        var command = new UpdateMovieListCommand { MovieListId = _movieListId, Name = "New Name" };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId, IsWatchlist = true, Name = "Watchlist" };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.CannotRenameWatchlist");
    }
}
