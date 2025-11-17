using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Auth.Commands.VerifyEmail;
using CineSocial.Domain.Entities.User;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.UnitTests.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<VerifyEmailCommandHandler>> _loggerMock;
    private readonly VerifyEmailCommandHandler _handler;
    private readonly string _token = "valid-token";

    public VerifyEmailCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<VerifyEmailCommandHandler>>();

        _handler = new VerifyEmailCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    private AppUser CreateTestUser(bool isConfirmed, DateTime? expiry)
    {
        return new AppUser
        {
            EmailVerificationToken = _token,
            EmailConfirmed = isConfirmed,
            EmailVerificationTokenExpiry = expiry
        };
    }

    [Fact]
    public async Task Should_VerifyEmail_When_TokenIsValidAndNotExpired()
    {
        // Arrange
        var command = new VerifyEmailCommand { Token = _token };
        var user = CreateTestUser(isConfirmed: false, expiry: DateTime.UtcNow.AddHours(1));

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.Repository<AppUser>().Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TokenIsInvalid()
    {
        // Arrange
        var command = new VerifyEmailCommand { Token = "invalid-token" };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AppUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidToken");
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_TokenIsExpired()
    {
        // Arrange
        var command = new VerifyEmailCommand { Token = _token };
        var user = CreateTestUser(isConfirmed: false, expiry: DateTime.UtcNow.AddHours(-1));

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.TokenExpired");
    }
}
