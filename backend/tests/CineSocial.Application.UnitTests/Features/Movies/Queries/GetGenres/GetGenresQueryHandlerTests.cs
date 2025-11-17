using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Movies.Queries.GetGenres;
using CineSocial.Domain.Entities.Movie;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.UnitTests.Features.Movies.Queries.GetGenres;

public class GetGenresQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly GetGenresQueryHandler _handler;

    public GetGenresQueryHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new GetGenresQueryHandler(_contextMock.Object);
    }

    private DbSet<Genre> CreateDbSet(List<Genre> genres)
    {
        var queryable = genres.AsQueryable();
        var dbSet = new Mock<DbSet<Genre>>();
        dbSet.As<IQueryable<Genre>>().Setup(m => m.Provider).Returns(queryable.Provider);
        dbSet.As<IQueryable<Genre>>().Setup(m => m.Expression).Returns(queryable.Expression);
        dbSet.As<IQueryable<Genre>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        dbSet.As<IQueryable<Genre>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        return dbSet.Object;
    }

    [Fact]
    public async Task Should_ReturnAllGenres_When_GenresExist()
    {
        // Arrange
        var genres = new List<Genre>
        {
            new Genre { Id = 1, Name = "Action" },
            new Genre { Id = 2, Name = "Comedy" }
        };
        _contextMock.Setup(c => c.Genres).Returns(CreateDbSet(genres));
        var query = new GetGenresQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoGenresExist()
    {
        // Arrange
        var genres = new List<Genre>();
        _contextMock.Setup(c => c.Genres).Returns(CreateDbSet(genres));
        var query = new GetGenresQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
