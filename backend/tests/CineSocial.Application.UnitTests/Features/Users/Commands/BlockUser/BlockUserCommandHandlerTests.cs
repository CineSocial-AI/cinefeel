using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Users.Commands.BlockUser;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.UnitTests.Features.Users.Commands.BlockUser;

public class BlockUserCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly BlockUserCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public BlockUserCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new BlockUserCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_SuccessfullyBlockUser_And_RemoveFollows()
    {
        // Arrange
        var command = new BlockUserCommand { TargetUserId = _targetUserId };
        var follow = new Follow { FollowerId = _currentUserId, FollowingId = _targetUserId };
        var followBack = new Follow { FollowerId = _targetUserId, FollowingId = _currentUserId };
        var follows = new List<Follow> { follow, followBack }.AsQueryable();

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(u => u.Id == _targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Block>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Block, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Block)null);

        // Mocking ToListAsync for Where clause is tricky, this is a common workaround
        _unitOfWorkMock.Setup(u => u.Repository<Follow>().Query()).Returns(follows.BuildMock());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<Block>().AddAsync(It.IsAny<Block>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.Repository<Follow>().HardDelete(It.IsAny<Follow>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Other test cases (unauthorized, block self, user not found, already blocked) are similar to FollowUserCommandHandlerTests
}
// Helper for mocking EF Core extensions on IQueryable
public static class MockQueryableExtensions
{
    public static DbSet<T> BuildMock<T>(this IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        var asyncData = new MockAsyncEnumerable<T>(data);
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncData.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(asyncData.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(asyncData.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(asyncData.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(asyncData.GetAsyncEnumerator());
        return mockSet.Object;
    }
}
public class MockAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>
{
    public MockAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public MockAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new MockAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
}
public class MockAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public T Current => _inner.Current;
    public MockAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}
