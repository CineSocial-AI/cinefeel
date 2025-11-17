using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Commands.FavoriteMovieList;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Commands.FavoriteMovieList;

public class FavoriteMovieListCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FavoriteMovieListCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly Guid _movieListId = Guid.NewGuid();

    public FavoriteMovieListCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new FavoriteMovieListCommandHandler(_unitOfWorkMock.Object, _currentUserServiceMock.Object);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_FavoriteList_When_ListIsPublicAndNotOwned()
    {
        // Arrange
        var command = new FavoriteMovieListCommand { MovieListId = _movieListId };
        var movieList = new MovieList { Id = _movieListId, UserId = _ownerUserId, IsPublic = true, FavoriteCount = 0 };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);
        _unitOfWorkMock.Setup(u => u.Repository<MovieListFavorite>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieListFavorite, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((MovieListFavorite)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        movieList.FavoriteCount.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.Repository<MovieListFavorite>().AddAsync(It.IsAny<MovieListFavorite>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Repository<MovieList>().Update(movieList), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_FavoritingOwnList()
    {
        // Arrange
        var command = new FavoriteMovieListCommand { MovieListId = _movieListId };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId }; // User owns the list

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.CannotFavoriteOwnList");
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_ListIsNotPublic()
    {
        // Arrange
        var command = new FavoriteMovieListCommand { MovieListId = _movieListId };
        var movieList = new MovieList { Id = _movieListId, UserId = _ownerUserId, IsPublic = false }; // Private list

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.NotPublic");
    }
}
