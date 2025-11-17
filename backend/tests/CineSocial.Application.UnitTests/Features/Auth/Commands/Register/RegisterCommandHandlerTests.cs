using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.Auth.Commands.Register;
using CineSocial.Domain.Entities.User;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IJobSchedulerService> _jobSchedulerMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtServiceMock = new Mock<IJwtService>();
        _jobSchedulerMock = new Mock<IJobSchedulerService>();

        _handler = new RegisterCommandHandler(
            _unitOfWorkMock.Object,
            _jwtServiceMock.Object,
            _jobSchedulerMock.Object
        );
    }

    // Test methods will be added here
    [Fact]
    public async Task Should_RegisterUser_When_UsernameAndEmailAreUnique()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(),
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(false);

        _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<AppUser>())).Returns("mock_jwt_token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Token.Should().Be("mock_jwt_token");

        _unitOfWorkMock.Verify(u => u.Repository<AppUser>().AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jobSchedulerMock.Verify(j => j.ScheduleEmailVerificationJobAsync(command.Email, command.Username, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _jwtServiceMock.Verify(j => j.GenerateToken(It.IsAny<AppUser>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_UsernameIsTaken()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(
            It.Is<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(expr => expr.ToString().Contains("Username")),
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.UsernameExists");

        _unitOfWorkMock.Verify(u => u.Repository<AppUser>().AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_EmailIsTaken()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(
            It.Is<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(expr => expr.ToString().Contains("Username")),
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(false);

        _unitOfWorkMock.Setup(u => u.Repository<AppUser>().Query().AnyAsync(
            It.Is<System.Linq.Expressions.Expression<System.Func<AppUser, bool>>>(expr => expr.ToString().Contains("Email")),
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.EmailExists");

        _unitOfWorkMock.Verify(u => u.Repository<AppUser>().AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
