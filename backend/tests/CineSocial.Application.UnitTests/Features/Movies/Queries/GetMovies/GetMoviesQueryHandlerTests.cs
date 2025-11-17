using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Queries.GetMovies;
using CineSocial.Domain.Entities.Movie;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace CineSocial.Application.UnitTests.Features.Movies.Queries.GetMovies;

public class GetMoviesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GetMoviesQueryHandler _handler;

    public GetMoviesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new GetMoviesQueryHandler(_unitOfWorkMock.Object);
    }

    private List<MovieEntity> CreateTestMovies()
    {
        return new List<MovieEntity>
        {
            new MovieEntity { Id = 1, Title = "Movie 1", Popularity = 10, ReleaseDate = new System.DateTime(2022, 1, 1), VoteAverage = 8.5, MovieGenres = new List<MovieGenre> { new MovieGenre { GenreId = 1, Genre = new Genre { Id = 1, Name = "Action" } } } },
            new MovieEntity { Id = 2, Title = "Another Movie", Popularity = 8, ReleaseDate = new System.DateTime(2021, 1, 1), VoteAverage = 7.5, MovieGenres = new List<MovieGenre> { new MovieGenre { GenreId = 2, Genre = new Genre { Id = 2, Name = "Comedy" } } } },
            new MovieEntity { Id = 3, Title = "The Third Movie", Popularity = 9, ReleaseDate = new System.DateTime(2022, 5, 10), VoteAverage = 9.0, MovieGenres = new List<MovieGenre> { new MovieGenre { GenreId = 1, Genre = new Genre { Id = 1, Name = "Action" } } } }
        };
    }

    [Fact]
    public async Task Should_ReturnPagedMovies_When_MoviesExist()
    {
        // Arrange
        var query = new GetMoviesQuery();
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoMoviesExist()
    {
        // Arrange
        var query = new GetMoviesQuery();
        var testMovies = new List<MovieEntity>().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnFilteredMovies_When_SearchQueryIsProvided()
    {
        // Arrange
        var query = new GetMoviesQuery { Search = "Another" };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Another Movie");
    }

     [Fact]
    public async Task Should_ReturnFilteredMovies_When_GenreIdIsProvided()
    {
        // Arrange
        var query = new GetMoviesQuery { GenreId = 2 };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Genres.First().Should().Be("Comedy");
    }

    [Fact]
    public async Task Should_ReturnFilteredMovies_When_YearIsProvided()
    {
        // Arrange
        var query = new GetMoviesQuery { Year = 2021 };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().ReleaseDate.Value.Year.Should().Be(2021);
    }

    [Fact]
    public async Task Should_ReturnSortedMovies_When_SortByTitleIsProvided()
    {
        // Arrange
        var query = new GetMoviesQuery { SortBy = MovieSortBy.Title };
        var testMovies = CreateTestMovies().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(testMovies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First().Title.Should().Be("Another Movie");
    }
}
