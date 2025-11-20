using MediatR;
using Microsoft.EntityFrameworkCore;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;

namespace CineSocial.Application.Features.Movies.Queries.GetMovieRecommendations;

public class GetMovieRecommendationsQueryHandler : IRequestHandler<GetMovieRecommendationsQuery, Result<List<MovieRecommendationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMovieRecommendationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<MovieRecommendationDto>>> Handle(
        GetMovieRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        // Get the source movie
        var sourceMovie = await _unitOfWork.Repository<MovieEntity>()
            .Query()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.Id == request.MovieId, cancellationToken);

        if (sourceMovie == null)
        {
            return Error.NotFound("Movie.NotFound", $"Movie with ID {request.MovieId} not found");
        }

        if (sourceMovie.ContentEmbedding == null)
        {
            return Error.Validation(
                "Movie.EmbeddingNotAvailable",
                "Movie embedding not available. Please ensure embeddings are generated for movies.");
        }

        // Get all movies with embeddings 
        // Note: In production with large datasets, you'd want to use pgvector's HNSW index
        // and query directly in the database. For now, we'll use a simple approach.
        var moviesWithEmbeddings = await _unitOfWork.Repository<MovieEntity>()
            .Query()
            .Where(m => m.Id != request.MovieId && m.ContentEmbedding != null)
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .ToListAsync(cancellationToken);

        // Calculate cosine similarity in memory
        var recommendations = moviesWithEmbeddings
            .Select(m => new
            {
                Movie = m,
                Similarity = CalculateCosineSimilarity(sourceMovie.ContentEmbedding, m.ContentEmbedding!)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(request.Limit)
            .ToList();

        var result = recommendations.Select(r => new MovieRecommendationDto
        {
            Id = r.Movie.Id,
            TmdbId = r.Movie.TmdbId,
            Title = r.Movie.Title,
            Overview = r.Movie.Overview,
            PosterPath = r.Movie.PosterPath,
            BackdropPath = r.Movie.BackdropPath,
            ReleaseDate = r.Movie.ReleaseDate,
            VoteAverage = r.Movie.VoteAverage,
            Popularity = r.Movie.Popularity,
            SimilarityScore = r.Similarity,
            Genres = r.Movie.MovieGenres
                .Where(mg => mg.Genre != null)
                .Select(mg => mg.Genre!.Name)
                .ToList()
        }).ToList();

        return Result<List<MovieRecommendationDto>>.Success(result);
    }

    private static double CalculateCosineSimilarity(Pgvector.Vector vec1, Pgvector.Vector vec2)
    {
        var a = vec1.ToArray();
        var b = vec2.ToArray();
        
        if (a.Length != b.Length)
            return 0;

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (magnitudeA * magnitudeB);
    }
}
