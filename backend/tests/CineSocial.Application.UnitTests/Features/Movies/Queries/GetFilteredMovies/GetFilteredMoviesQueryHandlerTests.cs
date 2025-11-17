using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Queries.GetFilteredMovies;
using CineSocial.Application.Features.Movies.Queries.GetMovies;
using CineSocial.Domain.Entities.Movie;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace CineSocial.Application.UnitTests.Features.Movies.Queries.GetFilteredMovies;

public class GetFilteredMoviesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GetFilteredMoviesQueryHandler _handler;

    public GetFilteredMoviesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new GetFilteredMoviesQueryHandler(_unitOfWorkMock.Object);
    }

    private List<MovieEntity> CreateTestMovies()
    {
        return new List<MovieEntity>
        {
            new MovieEntity { Id = 1, Title = "Movie 1", ReleaseDate = new System.DateTime(2022, 1, 1), VoteAverage = 8.5, OriginalLanguage = "en" },
            new MovieEntity { Id = 2, Title = "Movie 2", ReleaseDate = new System.DateTime(2019, 1, 1), VoteAverage = 7.5, OriginalLanguage = "es" },
            new MovieEntity { Id = 3, Title = "Movie 3", ReleaseDate = new System.DateTime(1995, 1, 1), VoteAverage = 9.0, OriginalLanguage = "en" }
        };
    }

    [Fact]
    public async Task Should_ReturnFilteredMovies_When_DecadeIsProvided()
    {
        // Arrange
        var query = new GetFilteredMoviesQuery { Decade = "1990s" };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Movie 3");
    }

    [Fact]
    public async Task Should_ReturnFilteredMovies_When_YearRangeIsProvided()
    {
        // Arrange
        var query = new GetFilteredMoviesQuery { YearFrom = 2020, YearTo = 2023 };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Movie 1");
    }

    [Fact]
    public async Task Should_ReturnFilteredMovies_When_RatingIsProvided()
    {
        // Arrange
        var query = new GetFilteredMoviesQuery { MinRating = 8.0, MaxRating = 9.0 };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnFilteredMovies_When_LanguageIsProvided()
    {
        // Arrange
        var query = new GetFilteredMoviesQuery { Language = "es" };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Movie 2");
    }
}
