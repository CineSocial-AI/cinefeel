using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Queries.GetFavoriteMovies;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Entities.Movie;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace CineSocial.Application.UnitTests.Features.Movies.Queries.GetFavoriteMovies;

public class GetFavoriteMoviesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetFavoriteMoviesQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetFavoriteMoviesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new GetFavoriteMoviesQueryHandler(_unitOfWorkMock.Object, _currentUserServiceMock.Object);
    }

    private List<MovieFavorite> CreateTestFavorites()
    {
        return new List<MovieFavorite>
        {
            new MovieFavorite { UserId = _userId, MovieId = 1, Movie = new MovieEntity { Id = 1, Title = "Favorite Movie 1" }, CreatedAt = DateTime.UtcNow },
            new MovieFavorite { UserId = _userId, MovieId = 2, Movie = new MovieEntity { Id = 2, Title = "Favorite Movie 2" }, CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };
    }

    [Fact]
    public async Task Should_ReturnFavoriteMovies_When_UserIsAuthenticatedAndHasFavorites()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        var query = new GetFavoriteMoviesQuery();
        var testFavorites = CreateTestFavorites().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieFavorite>().Query()).Returns(testFavorites.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_UserHasNoFavorites()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        var query = new GetFavoriteMoviesQuery();
        var testFavorites = new List<MovieFavorite>().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieFavorite>().Query()).Returns(testFavorites.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);
        var query = new GetFavoriteMoviesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.Unauthorized");
    }
}
