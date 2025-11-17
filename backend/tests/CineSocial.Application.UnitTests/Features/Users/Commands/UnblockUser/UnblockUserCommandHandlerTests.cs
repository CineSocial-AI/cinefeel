using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Users.Commands.UnblockUser;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Users.Commands.UnblockUser;

public class UnblockUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UnblockUserCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public UnblockUserCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new UnblockUserCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_SuccessfullyUnblockUser_When_UserIsBlocked()
    {
        // Arrange
        var command = new UnblockUserCommand { TargetUserId = _targetUserId };
        var existingBlock = new Block { BlockerId = _currentUserId, BlockedUserId = _targetUserId };

        _unitOfWorkMock.Setup(u => u.Repository<Block>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Block, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingBlock);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<Block>().HardDelete(existingBlock), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserIsNotBlocked()
    {
        // Arrange
        var command = new UnblockUserCommand { TargetUserId = _targetUserId };

        _unitOfWorkMock.Setup(u => u.Repository<Block>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Block, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Block)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Block.NotFound");
    }
}
