using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Queries.GetMovieListDetail;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Entities.User;
using CineSocial.Domain.Entities.Movie;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Queries.GetMovieListDetail;

public class GetMovieListDetailQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetMovieListDetailQueryHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly Guid _movieListId = Guid.NewGuid();

    public GetMovieListDetailQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new GetMovieListDetailQueryHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private MovieList CreateTestMovieList(bool isPublic)
    {
        return new MovieList
        {
            Id = _movieListId,
            UserId = _ownerUserId,
            User = new AppUser { Id = _ownerUserId, Username = "owner" },
            IsPublic = isPublic,
            Items = new List<MovieListItem>
            {
                new MovieListItem { Movie = new MovieEntity { Id = 1, Title = "Movie 1"}, Order = 1 }
            }
        };
    }

    [Fact]
    public async Task Should_ReturnListDetail_When_ListIsPublic()
    {
        // Arrange
        var query = new GetMovieListDetailQuery { MovieListId = _movieListId };
        var testList = CreateTestMovieList(isPublic: true);
        var lists = new List<MovieList> { testList }.AsQueryable();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query()).Returns(lists.BuildMock());
        _unitOfWorkMock.Setup(u => u.Repository<MovieListFavorite>().Query().AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<MovieListFavorite, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(testList.Name);
        result.Value.Movies.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_ListIsPrivateAndUserIsNotOwner()
    {
        // Arrange
        var query = new GetMovieListDetailQuery { MovieListId = _movieListId };
        var testList = CreateTestMovieList(isPublic: false);
        var lists = new List<MovieList> { testList }.AsQueryable();

        // Current user is not the owner
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
        _unitOfWorkMock.Setup(u => u.Repository<MovieList>().Query()).Returns(lists.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("MovieList.Private");
    }
}
