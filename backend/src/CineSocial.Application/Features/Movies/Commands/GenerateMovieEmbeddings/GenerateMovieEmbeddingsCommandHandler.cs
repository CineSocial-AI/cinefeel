using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;

namespace CineSocial.Application.Features.Movies.Commands.GenerateMovieEmbeddings;

/// <summary>
/// Handler for generating content embeddings for movies.
/// This command should be run after importing new movies to enable content-based recommendations.
/// </summary>
public class GenerateMovieEmbeddingsCommandHandler : IRequestHandler<GenerateMovieEmbeddingsCommand, Result<GenerateMovieEmbeddingsResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateMovieEmbeddingsCommandHandler> _logger;
    private readonly IMovieEmbeddingService _embeddingService;

    public GenerateMovieEmbeddingsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<GenerateMovieEmbeddingsCommandHandler> logger,
        IMovieEmbeddingService embeddingService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _embeddingService = embeddingService;
    }

    public async Task<Result<GenerateMovieEmbeddingsResult>> Handle(
        GenerateMovieEmbeddingsCommand request,
        CancellationToken cancellationToken)
    {
        var result = new GenerateMovieEmbeddingsResult();

        try
        {
            // Get movies that need embeddings
            var query = _unitOfWork.Repository<MovieEntity>().Query();

            if (!request.Force)
            {
                // Only process movies without embeddings
                query = query.Where(m => m.ContentEmbedding == null);
            }

            var movies = await query
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieKeywords)
                    .ThenInclude(mk => mk.Keyword)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Generating embeddings for {Count} movies", movies.Count);

            foreach (var movie in movies)
            {
                try
                {
                    result.ProcessedCount++;

                    // Generate embedding
                    var embedding = _embeddingService.GenerateEmbedding(movie);
                    movie.ContentEmbedding = embedding;

                    result.SuccessCount++;

                    // Save in batches of 100 to avoid holding too much in memory
                    if (result.ProcessedCount % 100 == 0)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Processed {Count} movies", result.ProcessedCount);
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    var errorMsg = $"Error processing movie {movie.Title} (ID: {movie.Id}): {ex.Message}";
                    result.Errors.Add(errorMsg);
                    _logger.LogError(ex, errorMsg);
                }
            }

            // Save any remaining changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Embedding generation complete. Processed: {Processed}, Success: {Success}, Errors: {Errors}",
                result.ProcessedCount, result.SuccessCount, result.ErrorCount);

            return Result<GenerateMovieEmbeddingsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate movie embeddings");
            return Error.Failure("Embedding.GenerationFailed", "Failed to generate movie embeddings: " + ex.Message);
        }
    }
}
