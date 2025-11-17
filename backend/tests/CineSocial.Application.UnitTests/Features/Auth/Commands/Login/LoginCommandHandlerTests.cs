using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Auth.Commands.Login;
using CineSocial.Domain.Entities.User;
using System.Threading.Tasks;
using System.Threading;
using CineSocial.Application.Common.Security;

namespace CineSocial.Application.UnitTests.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtServiceMock = new Mock<IJwtService>();

        _handler = new LoginCommandHandler(
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object
        );
    }

    private AppUser CreateTestUser(bool isActive = true, bool emailConfirmed = true)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123!"),
            IsActive = isActive,
            EmailConfirmed = emailConfirmed
        };
    }

    [Fact]
    public async Task Should_ReturnSuccessAndToken_When_CredentialsAreValid()
    {
        // Arrange
        var command = new LoginCommand { UsernameOrEmail = "testuser", Password = "Password123!" };
        var testUser = CreateTestUser();

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(testUser);
        _jwtServiceMock.Setup(j => j.GenerateToken(testUser)).Returns("mock_jwt_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("mock_jwt_token");
        _unitOfWorkMock.Verify(u => u.Repository<AppUser>().Update(testUser), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserNotFound()
    {
        // Arrange
        var command = new LoginCommand { UsernameOrEmail = "nonexistinguser", Password = "Password123!" };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync((AppUser)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_PasswordIsIncorrect()
    {
        // Arrange
        var command = new LoginCommand { UsernameOrEmail = "testuser", Password = "WrongPassword!" };
        var testUser = CreateTestUser();

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(testUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_UserIsInactive()
    {
        // Arrange
        var command = new LoginCommand { UsernameOrEmail = "testuser", Password = "Password123!" };
        var testUser = CreateTestUser(isActive: false);

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(testUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.UserInactive");
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_EmailIsNotConfirmed()
    {
        // Arrange
        var command = new LoginCommand { UsernameOrEmail = "testuser", Password = "Password123!" };
        var testUser = CreateTestUser(emailConfirmed: false);

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(testUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.EmailNotConfirmed");
    }
}
