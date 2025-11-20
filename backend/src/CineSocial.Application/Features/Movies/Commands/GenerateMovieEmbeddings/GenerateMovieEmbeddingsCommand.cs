using MediatR;
using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Features.Movies.Commands.GenerateMovieEmbeddings;

/// <summary>
/// Command to generate content embeddings for all movies that don't have embeddings yet.
/// </summary>
public class GenerateMovieEmbeddingsCommand : IRequest<Result<GenerateMovieEmbeddingsResult>>
{
    /// <summary>
    /// If true, regenerates embeddings for all movies, even those that already have embeddings.
    /// </summary>
    public bool Force { get; set; } = false;
}

public class GenerateMovieEmbeddingsResult
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
