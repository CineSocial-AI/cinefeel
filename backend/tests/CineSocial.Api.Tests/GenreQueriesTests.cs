using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CineSocial.Api.Tests;

public class GenreQueriesTests : IClassFixture<GraphQLTestFactory>
{
    private readonly HttpClient _client;
    private readonly GraphQLTestFactory _factory;

    public GenreQueriesTests(GraphQLTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGenres_ShouldReturnAllGenres()
    {
        // Arrange
        var query = new
        {
            query = @"
                query {
                    genres {
                        id
                        tmdbId
                        name
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if data exists
        result.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("genres", out var genres).Should().BeTrue();

        // Verify genres array
        var genresArray = genres.EnumerateArray().ToList();
        genresArray.Should().HaveCountGreaterThanOrEqualTo(3);

        // Verify first genre data
        var firstGenre = genresArray.First();
        firstGenre.TryGetProperty("id", out var id).Should().BeTrue();
        firstGenre.TryGetProperty("tmdbId", out var tmdbId).Should().BeTrue();
        firstGenre.TryGetProperty("name", out var name).Should().BeTrue();

        // Verify expected genres exist
        var genreNames = genresArray
            .Select(g => g.GetProperty("name").GetString())
            .ToList();

        genreNames.Should().Contain(new[] { "Action", "Comedy", "Drama" });
    }

    [Fact]
    public async Task GetGenreById_ShouldReturnSpecificGenre()
    {
        // Arrange
        var query = new
        {
            query = @"
                query {
                    genreById(id: 1) {
                        id
                        tmdbId
                        name
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Check if data exists
        result.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("genreById", out var genre).Should().BeTrue();

        // Verify genre data
        genre.GetProperty("id").GetInt32().Should().Be(1);
        genre.GetProperty("tmdbId").GetInt32().Should().Be(28);
        genre.GetProperty("name").GetString().Should().Be("Action");
    }
}
