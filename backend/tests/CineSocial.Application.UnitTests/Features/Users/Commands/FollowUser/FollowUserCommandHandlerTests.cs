using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Users.Commands.FollowUser;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Users.Commands.FollowUser;

public class FollowUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FollowUserCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public FollowUserCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new FollowUserCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_SuccessfullyFollowUser_When_UserIsValidAndNotAlreadyFollowing()
    {
        // Arrange
        var command = new FollowUserCommand { TargetUserId = _targetUserId };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(u => u.Id == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Follow>().Query().FirstOrDefaultAsync(f => f.FollowerId == _currentUserId && f.FollowingId == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync((Follow)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<Follow>().AddAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        var command = new FollowUserCommand { TargetUserId = _targetUserId };
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.Unauthorized");
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_UserTriesToFollowSelf()
    {
        // Arrange
        var command = new FollowUserCommand { TargetUserId = _currentUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Follow.CannotFollowSelf");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TargetUserDoesNotExist()
    {
        // Arrange
        var command = new FollowUserCommand { TargetUserId = _targetUserId };
        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(u => u.Id == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_UserIsAlreadyFollowing()
    {
        // Arrange
        var command = new FollowUserCommand { TargetUserId = _targetUserId };
        var existingFollow = new Follow { FollowerId = _currentUserId, FollowingId = _targetUserId };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(u => u.Id == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Follow>().Query().FirstOrDefaultAsync(f => f.FollowerId == _currentUserId && f.FollowingId == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(existingFollow);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.Repository<Follow>().AddAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
