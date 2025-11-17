using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Comments.Commands.UpdateComment;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Comments.Commands.UpdateComment;

public class UpdateCommentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly UpdateCommentCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _commentId = Guid.NewGuid();

    public UpdateCommentCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new UpdateCommentCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_UpdateComment_When_UserIsOwner()
    {
        // Arrange
        var command = new UpdateCommentCommand { CommentId = _commentId, Content = "Updated content" };
        var comment = new Comment { Id = _commentId, UserId = _currentUserId, Content = "Old content" };

        _unitOfWorkMock.Setup(u => u.Repository<Comment>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Comment, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(comment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        comment.Content.Should().Be("Updated content");
        comment.IsEdited.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.Repository<Comment>().Update(comment), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_UserIsNotOwner()
    {
        // Arrange
        var command = new UpdateCommentCommand { CommentId = _commentId, Content = "Updated content" };
        var comment = new Comment { Id = _commentId, UserId = Guid.NewGuid() }; // Different user

        _unitOfWorkMock.Setup(u => u.Repository<Comment>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Comment, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(comment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Comment.NotOwner");
    }
}
