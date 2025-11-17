using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.People.Queries.GetPersonDetail;
using CineSocial.Domain.Entities.Movie;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace CineSocial.Application.UnitTests.Features.People.Queries.GetPersonDetail;

public class GetPersonDetailQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GetPersonDetailQueryHandler _handler;
    private readonly int _personId = 1;

    public GetPersonDetailQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new GetPersonDetailQueryHandler(_unitOfWorkMock.Object);
    }

    private Person CreateTestPerson()
    {
        return new Person
        {
            Id = _personId,
            Name = "Test Person",
            MovieCasts = new List<MovieCast>
            {
                new MovieCast { MovieId = 1, Movie = new MovieEntity { Id = 1, Title = "Movie 1"}, Character = "Role 1" }
            },
            MovieCrews = new List<MovieCrew>
            {
                new MovieCrew { MovieId = 2, Movie = new MovieEntity { Id = 2, Title = "Movie 2"}, Job = "Director" }
            }
        };
    }

    [Fact]
    public async Task Should_ReturnPersonDetail_When_PersonExists()
    {
        // Arrange
        var query = new GetPersonDetailQuery { Id = _personId };
        var testPerson = CreateTestPerson();
        var people = new List<Person> { testPerson }.AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<Person>().Query()).Returns(people.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Person");
        result.Value.CastCredits.Should().HaveCount(1);
        result.Value.CastCredits.First().Title.Should().Be("Movie 1");
        result.Value.CrewCredits.Should().HaveCount(1);
        result.Value.CrewCredits.First().Job.Should().Be("Director");
    }
}
