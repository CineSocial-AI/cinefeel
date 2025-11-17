using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Users.Queries.GetUserProfile;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace CineSocial.Application.UnitTests.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetUserProfileQueryHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public GetUserProfileQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new GetUserProfileQueryHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private AppUser CreateTestUser()
    {
        return new AppUser
        {
            Id = _targetUserId,
            Username = "targetuser",
            Email = "target@example.com",
            Followers = new List<Follow>(),
            Following = new List<Follow>()
        };
    }

    [Fact]
    public async Task Should_ReturnUserProfile_When_UserExists()
    {
        // Arrange
        var query = new GetUserProfileQuery { UserId = _targetUserId };
        var testUser = CreateTestUser();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query()).Returns(new List<AppUser> { testUser }.AsQueryable().BuildMock());
        _unitOfWorkMock.Setup(u => u.Repository<Follow>().Query().AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Follow, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); // Mock isFollowing = true
        _unitOfWorkMock.Setup(u => u.Repository<Block>().Query().AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Block, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); // Mock isBlocked = false

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Username.Should().Be("targetuser");
        result.Value.IsFollowing.Should().BeTrue();
        result.Value.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserDoesNotExist()
    {
        // Arrange
        var query = new GetUserProfileQuery { UserId = _targetUserId };
        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query()).Returns(new List<AppUser>().AsQueryable().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Should_NotCheckFollowOrBlock_When_ViewingOwnProfile()
    {
        // Arrange
        var query = new GetUserProfileQuery { UserId = _currentUserId };
        var testUser = new AppUser { Id = _currentUserId, Followers = new List<Follow>(), Following = new List<Follow>() };
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query()).Returns(new List<AppUser> { testUser }.AsQueryable().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFollowing.Should().BeFalse();
        result.Value.IsBlocked.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.Repository<Follow>().Query().AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Follow, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.Repository<Block>().Query().AnyAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Block, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
