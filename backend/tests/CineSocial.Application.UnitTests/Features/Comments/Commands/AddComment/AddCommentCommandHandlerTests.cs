using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Comments.Commands.AddComment;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace CineSocial.Application.UnitTests.Features.Comments.Commands.AddComment;

public class AddCommentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AddCommentCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly AppUser _testUser = new AppUser { Id = Guid.NewGuid(), Username = "testuser" };
    private readonly int _movieId = 101;

    public AddCommentCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new AddCommentCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
        _testUser.Id = _currentUserId; // Align user id for the test
        var users = new List<AppUser> { _testUser }.AsQueryable();
        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query()).Returns(users.BuildMock());
    }

    [Fact]
    public async Task Should_AddTopLevelComment_When_ParentIdIsNull()
    {
        // Arrange
        var command = new AddCommentCommand { MovieId = _movieId, Content = "Great movie!" };
        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Great movie!");
        result.Value.Depth.Should().Be(0);
        result.Value.ParentCommentId.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.Repository<Comment>().AddAsync(It.Is<Comment>(c => c.Depth == 0), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_AddReplyComment_When_ParentIdIsValid()
    {
        // Arrange
        var parentCommentId = Guid.NewGuid();
        var command = new AddCommentCommand { MovieId = _movieId, Content = "I agree!", ParentCommentId = parentCommentId };
        var parentComment = new Comment { Id = parentCommentId, MovieId = _movieId, Depth = 0 };

        _unitOfWorkMock.Setup(u => u.Repository<MovieEntity>().Query().AnyAsync(m => m.Id == _movieId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Comment>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Comment, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(parentComment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("I agree!");
        result.Value.Depth.Should().Be(1);
        result.Value.ParentCommentId.Should().Be(parentCommentId);
        _unitOfWorkMock.Verify(u => u.Repository<Comment>().AddAsync(It.Is<Comment>(c => c.Depth == 1 && c.ParentCommentId == parentCommentId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
