using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Auth.Commands.ResendVerification;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using MediatR;

namespace CineSocial.Application.UnitTests.Features.Auth.Commands.ResendVerification;

public class ResendVerificationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJobSchedulerService> _jobSchedulerMock;
    private readonly ResendVerificationCommandHandler _handler;

    public ResendVerificationCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jobSchedulerMock = new Mock<IJobSchedulerService>();
        _handler = new ResendVerificationCommandHandler(_unitOfWorkMock.Object, _jobSchedulerMock.Object);
    }

    [Fact]
    public async Task Should_GenerateNewTokenAndScheduleJob_When_EmailIsNotConfirmed()
    {
        // Arrange
        var command = new ResendVerificationCommand { Email = "test@example.com" };
        var user = new AppUser { Email = "test@example.com", EmailConfirmed = false };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.EmailVerificationToken.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.Repository<AppUser>().Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jobSchedulerMock.Verify(j => j.ScheduleEmailVerificationJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnSuccessWithoutAction_When_EmailIsAlreadyConfirmed()
    {
        // Arrange
        var command = new ResendVerificationCommand { Email = "test@example.com" };
        var user = new AppUser { Email = "test@example.com", EmailConfirmed = true };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jobSchedulerMock.Verify(j => j.ScheduleEmailVerificationJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
