using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Queries.GetMovieDetail;
using CineSocial.Domain.Entities.Movie;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using CineSocial.Domain.Common;

namespace CineSocial.Application.UnitTests.Features.Movies.Queries.GetMovieDetail;

public class GetMovieDetailQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GetMovieDetailQueryHandler _handler;
    private readonly int _movieId = 1;

    public GetMovieDetailQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new GetMovieDetailQueryHandler(_unitOfWorkMock.Object);
    }

    private MovieEntity CreateTestMovie()
    {
        return new MovieEntity
        {
            Id = _movieId,
            Title = "Test Movie",
            MovieGenres = new List<MovieGenre>
            {
                new MovieGenre { GenreId = 1, Genre = new Genre { Id = 1, Name = "Action" } }
            },
            MovieCasts = new List<MovieCast>
            {
                new MovieCast { PersonId = 1, Person = new Person { Id = 1, Name = "Actor 1"}, Character = "Hero", CastOrder = 1 }
            },
            MovieCrews = new List<MovieCrew>
            {
                new MovieCrew { PersonId = 2, Person = new Person { Id = 2, Name = "Director 1"}, Job = "Director" }
            }
        };
    }

    [Fact]
    public async Task Should_ReturnMovieDetail_When_MovieExists()
    {
        // Arrange
        var query = new GetMovieDetailQuery { Id = _movieId };
        var testMovie = CreateTestMovie();
        var movies = new List<MovieEntity> { testMovie }.AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(movies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Movie");
        result.Value.Genres.Should().HaveCount(1);
        result.Value.Genres.First().Name.Should().Be("Action");
        result.Value.Cast.Should().HaveCount(1);
        result.Value.Cast.First().Name.Should().Be("Actor 1");
        result.Value.Crew.Should().HaveCount(1);
        result.Value.Crew.First().Job.Should().Be("Director");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_MovieDoesNotExist()
    {
        // Arrange
        var query = new GetMovieDetailQuery { Id = _movieId };
        var movies = new List<MovieEntity>().AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(movies.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Movie.NotFound");
    }
}
