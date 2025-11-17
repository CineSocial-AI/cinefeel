using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Rates.Commands.RateMovie;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Rates.Commands.RateMovie;

public class RateMovieCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly RateMovieCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly int _movieId = 789;

    public RateMovieCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new RateMovieCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_CreateNewRate_When_UserHasNotRatedBefore()
    {
        // Arrange
        var command = new RateMovieCommand { MovieId = _movieId, Rating = 8 };
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Rate>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Rate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Rate)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        result.Value.Rating.Should().Be(8);
        _unitOfWorkMock.Verify(u => u.Repository<Rate>().AddAsync(It.IsAny<Rate>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_UpdateExistingRate_When_UserHasRatedBefore()
    {
        // Arrange
        var command = new RateMovieCommand { MovieId = _movieId, Rating = 9 };
        var existingRate = new Rate { UserId = _currentUserId, MovieId = _movieId, Rating = 5 };
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Rate>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Rate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingRate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeFalse();
        result.Value.Rating.Should().Be(9);
        existingRate.Rating.Should().Be(9); // Verify the object was updated
        _unitOfWorkMock.Verify(u => u.Repository<Rate>().Update(existingRate), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
