using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Security;
using CineSocial.Application.Features.Auth.Commands.Login;
using CineSocial.Application.UnitTests.Common;
using CineSocial.Application.UnitTests.Common.Builders;
using CineSocial.Domain.Entities.User;

namespace CineSocial.Application.UnitTests.Features.Auth.Commands.Login;

public class LoginCommandHandlerTests : IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        var context = TestDatabaseFactory.CreateInMemoryDatabase();
        _unitOfWork = TestDatabaseFactory.CreateUnitOfWork(context);
        _jwtService = Substitute.For<IJwtService>();
        _handler = new LoginCommandHandler(_unitOfWork, _jwtService);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var password = "Test123!";
        var passwordHash = PasswordHasher.HashPassword(password);

        var user = new AppUserBuilder()
            .WithUsername("johndoe")
            .WithEmail("john@example.com")
            .WithPasswordHash(passwordHash)
            .Build();

        await _unitOfWork.Repository<AppUser>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var command = new LoginCommand("johndoe", password);
        _jwtService.GenerateToken(user).Returns("fake_jwt_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Username.Should().Be("johndoe");
        result.Value.Email.Should().Be("john@example.com");
        result.Value.Token.Should().Be("fake_jwt_token");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Contain("Invalid username/email or password");
    }

    [Fact]
    public async Task Handle_WithUnconfirmedEmail_ReturnsFailure()
    {
        // Arrange
        var password = "Test123!";
        var passwordHash = PasswordHasher.HashPassword(password);

        var user = new AppUserBuilder()
            .WithUsername("johndoe")
            .WithEmail("john@example.com")
            .WithPasswordHash(passwordHash)
            .EmailNotConfirmed()
            .Build();

        await _unitOfWork.Repository<AppUser>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var command = new LoginCommand("johndoe", password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.EmailNotConfirmed");
        result.Error.Description.Should().Contain("verify your email");
    }

    public void Dispose()
    {
        _unitOfWork?.Dispose();
    }
}
