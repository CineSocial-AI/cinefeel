using CineSocial.Application.Common.Interfaces;
using CineSocial.Domain.Entities.Movie;
using Pgvector;
using System.Security.Cryptography;
using System.Text;

namespace CineSocial.Infrastructure.Services;

/// <summary>
/// Service for generating content-based vector embeddings for movies.
/// This is a simple implementation using feature hashing.
/// In production, consider using proper embedding models like sentence transformers.
/// </summary>
public class MovieEmbeddingService : IMovieEmbeddingService
{
    private const int EmbeddingDimensions = 384;

    /// <summary>
    /// Generates a content-based embedding vector for a movie.
    /// Creates a 384-dimensional vector based on movie features like genres, overview, and keywords.
    /// </summary>
    public Vector GenerateEmbedding(MovieEntity movie)
    {
        var features = new List<string>();

        // Add title features
        if (!string.IsNullOrWhiteSpace(movie.Title))
        {
            features.AddRange(movie.Title.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Add overview features (tokenized)
        if (!string.IsNullOrWhiteSpace(movie.Overview))
        {
            var overviewWords = movie.Overview.ToLower()
                .Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(50); // Limit to first 50 words
            features.AddRange(overviewWords);
        }

        // Add genre features (with higher weight by repeating)
        foreach (var genre in movie.MovieGenres)
        {
            if (genre.Genre != null)
            {
                // Add genre name multiple times to give it more weight
                for (int i = 0; i < 5; i++)
                {
                    features.Add($"genre:{genre.Genre.Name.ToLower()}");
                }
            }
        }

        // Add keyword features (with weight)
        foreach (var keyword in movie.MovieKeywords.Take(10)) // Top 10 keywords
        {
            if (keyword.Keyword != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    features.Add($"keyword:{keyword.Keyword.Name.ToLower()}");
                }
            }
        }

        // Add language feature
        if (!string.IsNullOrWhiteSpace(movie.OriginalLanguage))
        {
            features.Add($"lang:{movie.OriginalLanguage.ToLower()}");
        }

        // Generate embedding using feature hashing
        var embedding = GenerateFeatureHashingEmbedding(features);

        return new Vector(embedding);
    }

    /// <summary>
    /// Generates an embedding using feature hashing (a simple but effective technique).
    /// Maps text features to a fixed-size vector using hash functions.
    /// </summary>
    private float[] GenerateFeatureHashingEmbedding(List<string> features)
    {
        var embedding = new float[EmbeddingDimensions];

        foreach (var feature in features)
        {
            // Use multiple hash functions for better distribution
            var hash1 = GetFeatureHash(feature, 1) % EmbeddingDimensions;
            var hash2 = GetFeatureHash(feature, 2) % EmbeddingDimensions;
            var hash3 = GetFeatureHash(feature, 3) % EmbeddingDimensions;

            // Get the sign hash to determine if we add or subtract
            var signHash = GetFeatureHash(feature, 0);
            var sign = (signHash % 2 == 0) ? 1f : -1f;

            // Update multiple dimensions for this feature
            embedding[hash1] += sign * 0.5f;
            embedding[hash2] += sign * 0.3f;
            embedding[hash3] += sign * 0.2f;
        }

        // Normalize the embedding to unit length (L2 normalization)
        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= (float)magnitude;
            }
        }

        return embedding;
    }

    /// <summary>
    /// Generates a hash value for a feature string with a given seed.
    /// </summary>
    private int GetFeatureHash(string feature, int seed)
    {
        var bytes = Encoding.UTF8.GetBytes(feature + seed.ToString());
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Math.Abs(BitConverter.ToInt32(hash, 0));
    }
}
