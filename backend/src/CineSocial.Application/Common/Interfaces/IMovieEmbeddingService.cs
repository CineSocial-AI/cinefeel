using CineSocial.Domain.Entities.Movie;
using Pgvector;

namespace CineSocial.Application.Common.Interfaces;

/// <summary>
/// Service for generating vector embeddings for movies based on their content features.
/// </summary>
public interface IMovieEmbeddingService
{
    /// <summary>
    /// Generates a content-based embedding vector for a movie.
    /// The embedding is based on features like genres, keywords, overview, and title.
    /// </summary>
    /// <param name="movie">The movie entity to generate an embedding for</param>
    /// <returns>A vector embedding representing the movie's content</returns>
    Vector GenerateEmbedding(MovieEntity movie);
}
