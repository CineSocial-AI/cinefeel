using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CineSocial.Infrastructure.Data;
using CineSocial.Infrastructure.Services;
using CineSocial.Domain.Entities.Movie;

namespace CineSocial.Infrastructure.Testing
{
    /// <summary>
    /// Test harness for the recommendation system.
    /// This can be run as a standalone console app or integrated into the API.
    /// </summary>
    public class RecommendationSystemTester
    {
        private readonly ApplicationDbContext _context;
        private readonly MovieEmbeddingService _embeddingService;
        private readonly ILogger _logger;

        public RecommendationSystemTester(
            ApplicationDbContext context,
            ILogger logger)
        {
            _context = context;
            _embeddingService = new MovieEmbeddingService();
            _logger = logger;
        }

        /// <summary>
        /// Main test workflow: Generate embeddings and test recommendations
        /// </summary>
        public async Task RunTestsAsync()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║    CineFeel Recommendation System Test Harness                ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            // Step 1: Check sample data
            await CheckSampleDataAsync();

            // Step 2: Generate embeddings
            await GenerateEmbeddingsAsync();

            // Step 3: Run recommendation tests
            await TestRecommendationsAsync();

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║    All Tests Complete!                                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        }

        private async Task CheckSampleDataAsync()
        {
            Console.WriteLine("Step 1: Checking sample data");
            Console.WriteLine("──────────────────────────────\n");

            var movieCount = await _context.Movies.CountAsync();
            Console.WriteLine($"✓ Found {movieCount} movies in database");

            if (movieCount == 0)
            {
                Console.WriteLine("⚠ No movies found! Please run the SQL script:");
                Console.WriteLine("  scripts/sample_movies.sql\n");
                return;
            }

            // Check for test movies
            var testMovies = new[] { "The Matrix", "Finding Nemo", "The Godfather", "Inception" };
            foreach (var title in testMovies)
            {
                var exists = await _context.Movies.AnyAsync(m => m.Title == title);
                Console.WriteLine($"{(exists ? "✓" : "✗")} {title}");
            }

            Console.WriteLine();
        }

        private async Task GenerateEmbeddingsAsync()
        {
            Console.WriteLine("Step 2: Generating embeddings");
            Console.WriteLine("──────────────────────────────\n");

            // Get movies without embeddings
            var movies = await _context.Movies
                .Where(m => m.ContentEmbedding == null)
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieKeywords)
                    .ThenInclude(mk => mk.Keyword)
                .ToListAsync();

            if (movies.Count == 0)
            {
                Console.WriteLine("✓ All movies already have embeddings\n");
                return;
            }

            Console.WriteLine($"Generating embeddings for {movies.Count} movies...\n");

            int processed = 0;
            foreach (var movie in movies)
            {
                try
                {
                    var embedding = _embeddingService.GenerateEmbedding(movie);
                    movie.ContentEmbedding = embedding;
                    processed++;

                    Console.WriteLine($"  [{processed}/{movies.Count}] {movie.Title}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error with {movie.Title}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"\n✓ Generated {processed} embeddings\n");
        }

        private async Task TestRecommendationsAsync()
        {
            Console.WriteLine("Step 3: Testing recommendations");
            Console.WriteLine("──────────────────────────────────\n");

            // Test cases based on expected similarity
            var testCases = new[]
            {
                new { Title = "The Matrix", ExpectedSimilar = new[] { "Inception", "Blade Runner 2049", "Interstellar" } },
                new { Title = "Finding Nemo", ExpectedSimilar = new[] { "Toy Story", "The Lion King", "Frozen" } },
                new { Title = "The Godfather", ExpectedSimilar = new[] { "Goodfellas", "Pulp Fiction" } },
                new { Title = "Ex Machina", ExpectedSimilar = new[] { "Her", "The Matrix", "Blade Runner 2049" } }
            };

            foreach (var testCase in testCases)
            {
                await TestMovieRecommendationsAsync(testCase.Title, testCase.ExpectedSimilar);
            }
        }

        private async Task TestMovieRecommendationsAsync(string movieTitle, string[] expectedSimilar)
        {
            Console.WriteLine($"Testing: {movieTitle}");
            Console.WriteLine(new string('─', 60));

            // Get the source movie
            var sourceMovie = await _context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Title == movieTitle);

            if (sourceMovie == null)
            {
                Console.WriteLine($"✗ Movie '{movieTitle}' not found\n");
                return;
            }

            if (sourceMovie.ContentEmbedding == null)
            {
                Console.WriteLine($"✗ Movie '{movieTitle}' has no embedding\n");
                return;
            }

            Console.WriteLine($"Genres: {string.Join(", ", sourceMovie.MovieGenres.Select(mg => mg.Genre?.Name))}");

            // Get recommendations (top 5)
            var recommendations = await _context.Movies
                .Where(m => m.Id != sourceMovie.Id && m.ContentEmbedding != null)
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .ToListAsync();

            var ranked = recommendations
                .Select(m => new
                {
                    Movie = m,
                    Similarity = CalculateCosineSimilarity(sourceMovie.ContentEmbedding, m.ContentEmbedding!)
                })
                .OrderByDescending(x => x.Similarity)
                .Take(5)
                .ToList();

            Console.WriteLine("\nTop 5 Recommendations:");
            for (int i = 0; i < ranked.Count; i++)
            {
                var rec = ranked[i];
                var genres = string.Join(", ", rec.Movie.MovieGenres.Select(mg => mg.Genre?.Name));
                var expectedMatch = expectedSimilar.Contains(rec.Movie.Title) ? "✓" : " ";
                
                Console.WriteLine($"{i + 1}. {expectedMatch} {rec.Movie.Title} (Score: {rec.Similarity:F3})");
                Console.WriteLine($"   Genres: {genres}");
            }

            // Check if expected movies are in top 5
            var foundExpected = ranked.Count(r => expectedSimilar.Contains(r.Movie.Title));
            Console.WriteLine($"\nExpected matches found: {foundExpected}/{Math.Min(expectedSimilar.Length, 5)}");
            
            if (foundExpected >= 2)
            {
                Console.WriteLine("✓ Test PASSED\n");
            }
            else
            {
                Console.WriteLine("⚠ Test needs review\n");
            }
        }

        private double CalculateCosineSimilarity(Pgvector.Vector vec1, Pgvector.Vector vec2)
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
}
