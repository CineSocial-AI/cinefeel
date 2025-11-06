using CineSocial.Application.Features.Auth.Commands.Login;
using CineSocial.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password
        );

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        var response = new AuthResponseDto
        {
            UserId = result.Value.UserId,
            Username = result.Value.Username,
            Email = result.Value.Email,
            Role = result.Value.Role,
            Token = result.Value.Token
        };

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "User registered successfully",
            Data = response
        });
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new LoginCommand(
            request.UsernameOrEmail,
            request.Password
        );

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        var response = new AuthResponseDto
        {
            UserId = result.Value.UserId,
            Username = result.Value.Username,
            Email = result.Value.Email,
            Role = result.Value.Role,
            Token = result.Value.Token
        };

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "Login successful",
            Data = response
        });
    }

    /// <summary>
    /// Get current user profile (requires authentication)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var profile = new CurrentUserDto
        {
            UserId = Guid.Parse(userId!),
            Username = username!,
            Email = email!,
            Role = role!
        };

        return Ok(new ApiResponse<CurrentUserDto>
        {
            Success = true,
            Message = "User profile retrieved successfully",
            Data = profile
        });
    }
}

// DTOs for API
public class RegisterRequestDto
{
    /// <summary>
    /// Username (3-50 characters, alphanumeric, underscore, hyphen)
    /// </summary>
    [Required]
    public string Username { get; set; } = "testuser";

    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "test@example.com";

    /// <summary>
    /// Password (minimum 6 characters)
    /// </summary>
    [Required]
    public string Password { get; set; } = "Password123";
}

public class LoginRequestDto
{
    /// <summary>
    /// Username or Email
    /// </summary>
    [Required]
    public string UsernameOrEmail { get; set; } = "testuser";

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    public string Password { get; set; } = "Password123";
}

public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? Error { get; set; }
}
