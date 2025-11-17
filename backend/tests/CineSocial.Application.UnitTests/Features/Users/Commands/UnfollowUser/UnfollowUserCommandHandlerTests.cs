using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Users.Commands.UnfollowUser;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using CineSocial.Application.Common.Results;

namespace CineSocial.Application.UnitTests.Features.Users.Commands.UnfollowUser;

public class UnfollowUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UnfollowUserCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public UnfollowUserCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new UnfollowUserCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_SuccessfullyUnfollowUser_When_UserIsFollowing()
    {
        // Arrange
        var command = new UnfollowUserCommand { TargetUserId = _targetUserId };
        var existingFollow = new Follow { FollowerId = _currentUserId, FollowingId = _targetUserId };

        _unitOfWorkMock.Setup(u => u.Repository<Follow>().Query().FirstOrDefaultAsync(f => f.FollowerId == _currentUserId && f.FollowingId == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(existingFollow);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<Follow>().HardDelete(existingFollow), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        var command = new UnfollowUserCommand { TargetUserId = _targetUserId };
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.Unauthorized");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserIsNotFollowing()
    {
        // Arrange
        var command = new UnfollowUserCommand { TargetUserId = _targetUserId };

        _unitOfWorkMock.Setup(u => u.Repository<Follow>().Query().FirstOrDefaultAsync(f => f.FollowerId == _currentUserId && f.FollowingId == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((Follow)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Follow.NotFound");
    }
}
