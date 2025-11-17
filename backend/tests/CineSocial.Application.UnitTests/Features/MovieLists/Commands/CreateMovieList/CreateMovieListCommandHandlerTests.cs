using Moq;
using Xunit;
using FluentAssertions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Features.MovieLists.Commands.CreateMovieList;
using CineSocial.Domain.Entities.Social;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace CineSocial.Application.UnitTests.Features.MovieLists.Commands.CreateMovieList;

public class CreateMovieListCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CreateMovieListCommandHandler _handler;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public CreateMovieListCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new CreateMovieListCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object
        );

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_currentUserId);
    }

    [Fact]
    public async Task Should_CreateMovieList_When_UserIsAuthenticated()
    {
        // Arrange
        var command = new CreateMovieListCommand { Name = "My awesome list", Description = "My awesome description", IsPublic = true };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(command.Name);
        _unitOfWorkMock.Verify(u => u.Repository<MovieList>().AddAsync(It.IsAny<MovieList>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        // Arrange
        var command = new CreateMovieListCommand { Name = "My awesome list", Description = "My awesome description", IsPublic = true };
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.Unauthorized");
    }
}
