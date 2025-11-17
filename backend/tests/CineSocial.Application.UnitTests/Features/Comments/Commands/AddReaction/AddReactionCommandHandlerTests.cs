using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Comments.Commands.AddReaction;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Enums;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Comments.Commands.AddReaction;

public class AddReactionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AddReactionCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _commentId = Guid.NewGuid();

    public AddReactionCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new AddReactionCommandHandler(_unitOfWorkMock.Object, _currentUserServiceMock.Object);
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_AddNewReaction_When_NoneExists()
    {
        // Arrange
        var command = new AddReactionCommand { CommentId = _commentId, ReactionType = ReactionType.Upvote };
        _unitOfWorkMock.Setup(u => u.Repository<Comment>().Query().AnyAsync(c => c.Id == _commentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Reaction>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Reaction, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((Reaction)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeTrue();
        result.Value.ReactionType.Should().Be("Upvote");
        _unitOfWorkMock.Verify(u => u.Repository<Reaction>().AddAsync(It.IsAny<Reaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_UpdateReaction_When_OneExists()
    {
        // Arrange
        var command = new AddReactionCommand { CommentId = _commentId, ReactionType = ReactionType.Downvote };
        var existingReaction = new Reaction { UserId = _currentUserId, CommentId = _commentId, Type = ReactionType.Upvote };
        _unitOfWorkMock.Setup(u => u.Repository<Comment>().Query().AnyAsync(c => c.Id == _commentId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.Repository<Reaction>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Reaction, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingReaction);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsNew.Should().BeFalse();
        result.Value.ReactionType.Should().Be("Downvote");
        existingReaction.Type.Should().Be(ReactionType.Downvote);
        _unitOfWorkMock.Verify(u => u.Repository<Reaction>().Update(existingReaction), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
