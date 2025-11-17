using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Rates.Commands.DeleteRate;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Rates.Commands.DeleteRate;

public class DeleteRateCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly DeleteRateCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly int _movieId = 999;

    public DeleteRateCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new DeleteRateCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_DeleteRate_When_RateExists()
    {
        // Arrange
        var command = new DeleteRateCommand { MovieId = _movieId };
        var rate = new Rate { UserId = _currentUserId, MovieId = _movieId, Rating = 8 };

        _unitOfWorkMock.Setup(u => u.Repository<Rate>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Rate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(rate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<Rate>().Delete(rate), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_RateDoesNotExist()
    {
        // Arrange
        var command = new DeleteRateCommand { MovieId = _movieId };

        _unitOfWorkMock.Setup(u => u.Repository<Rate>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Rate, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Rate)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Rate.NotFound");
    }
}
