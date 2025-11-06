using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CineSocial.Api.Tests;

public class CommentMutationsTests : IClassFixture<GraphQLTestFactory>
{
    private readonly HttpClient _client;
    private readonly GraphQLTestFactory _factory;

    public CommentMutationsTests(GraphQLTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateTestJwtToken(int userId = 100, string username = "testuser")
    {
        var jwtSecret = "test-secret-key-with-minimum-256-bits-for-testing-purposes-only";
        var jwtIssuer = "TestIssuer";
        var jwtAudience = "TestAudience";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task AddComment_WithValidData_ShouldCreateComment()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mutation = new
        {
            query = @"
                mutation {
                    addComment(movieId: 1, content: ""This is a test comment"") {
                        id
                        content
                        movieId
                        userId
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if data exists
        result.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("addComment", out var comment).Should().BeTrue();

        // Verify comment data
        comment.TryGetProperty("id", out var id).Should().BeTrue();
        id.GetInt32().Should().BeGreaterThan(0);

        comment.GetProperty("content").GetString().Should().Be("This is a test comment");
        comment.GetProperty("movieId").GetInt32().Should().Be(1);
        comment.GetProperty("userId").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task AddComment_WithoutAuthentication_ShouldFail()
    {
        // Arrange - No authentication token
        var mutation = new
        {
            query = @"
                mutation {
                    addComment(movieId: 1, content: ""This should fail"") {
                        id
                        content
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Should have errors because user is not authenticated
        result.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddReply_WithValidData_ShouldCreateReply()
    {
        // Arrange
        var token = GenerateTestJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // First create a parent comment
        var parentCommentMutation = new
        {
            query = @"
                mutation {
                    addComment(movieId: 1, content: ""Parent comment"") {
                        id
                    }
                }"
        };

        var parentResponse = await _client.PostAsJsonAsync("/graphql", parentCommentMutation);
        var parentContent = await parentResponse.Content.ReadAsStringAsync();
        var parentResult = JsonSerializer.Deserialize<JsonElement>(parentContent);
        var parentId = parentResult
            .GetProperty("data")
            .GetProperty("addComment")
            .GetProperty("id")
            .GetInt32();

        // Now add a reply to that comment
        var replyMutation = new
        {
            query = $@"
                mutation {{
                    addReply(parentCommentId: {parentId}, content: ""This is a reply"") {{
                        id
                        content
                        parentCommentId
                        userId
                    }}
                }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", replyMutation);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if data exists
        result.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("addReply", out var reply).Should().BeTrue();

        // Verify reply data
        reply.GetProperty("content").GetString().Should().Be("This is a reply");
        reply.GetProperty("parentCommentId").GetInt32().Should().Be(parentId);
        reply.GetProperty("userId").GetInt32().Should().Be(100);
    }
}
