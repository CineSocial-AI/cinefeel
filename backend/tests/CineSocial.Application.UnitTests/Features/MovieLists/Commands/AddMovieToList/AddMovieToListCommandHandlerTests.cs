using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Commands.AddMovieToList;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Commands.AddMovieToList;

public class AddMovieToListCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AddMovieToListCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _movieListId = Guid.NewGuid();
    private readonly Guid _movieId = Guid.NewGuid();

    public AddMovieToListCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new AddMovieToListCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_AddMovieToList_When_Valid()
    {
        // Arrange
        var command = new AddMovieToListCommand { MovieListId = _movieListId, MovieId = _movieId };
        var movieList = new MovieList { Id = _movieListId, UserId = _currentUserId };

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<MovieListItem>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieListItem, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((MovieListItem)null);
        _unitOfWorkMock.Setup(u => u.Repository<MovieListItem>().Query()).Returns(new List<MovieListItem>().AsQueryable().BuildMock()); // For MaxAsync

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<MovieListItem>().AddAsync(It.IsAny<MovieListItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_UserDoesNotOwnList()
    {
        // Arrange
        var command = new AddMovieToListCommand { MovieListId = _movieListId, MovieId = _movieId };
        var movieList = new MovieList { Id = _movieListId, UserId = Guid.NewGuid() }; // Different UserId

        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieList, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(movieList);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.Forbidden");
    }
}
