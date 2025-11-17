using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Search.Queries.SearchAll;
using CineSocial.Domain.Entities.Movie;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace CineSocial.Application.UnitTests.Features.Search.Queries.SearchAll;

public class SearchAllQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SearchAllQueryHandler _handler;

    public SearchAllQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new SearchAllQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Should_ReturnSearchResults_When_QueryIsProvided()
    {
        // Arrange
        var query = new SearchAllQuery { Query = "test" };
        var movies = new List<MovieEntity>
        {
            new MovieEntity { Title = "Test Movie 1" },
            new MovieEntity { Title = "Another Movie" }
        }.AsQueryable();
        var people = new List<Person>
        {
            new Person { Name = "Test Person 1" },
            new Person { Name = "Another Person" }
        }.AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query()).Returns(movies.BuildMock());
        _unitOfWorkMock.Setup(u => u.Repository<Person>().Query()).Returns(people.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Movies.Should().HaveCount(1);
        result.Value.Movies.First().Title.Should().Be("Test Movie 1");
        result.Value.People.Should().HaveCount(1);
        result.Value.People.First().Name.Should().Be("Test Person 1");
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_QueryIsEmpty()
    {
        // Arrange
        var query = new SearchAllQuery { Query = " " };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Search.EmptyQuery");
    }
}
