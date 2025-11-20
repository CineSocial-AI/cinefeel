# Content-Based Movie Recommendations with pgvector

This document describes the pgvector-based content recommendation system implemented in CineFeel.

## Overview

The recommendation system uses **pgvector** (PostgreSQL vector similarity search) to provide content-based movie recommendations. It generates embeddings for movies based on their content features (genres, keywords, overview, title) and uses cosine similarity to find similar movies.

## Architecture

### Components

1. **Vector Storage**: PostgreSQL with pgvector extension
   - Dimension: 384
   - Column: `ContentEmbedding` in `Movies` table
   - Type: `vector(384)`

2. **Embedding Generation**: `MovieEmbeddingService`
   - Uses feature hashing technique
   - Combines: title, overview, genres, keywords, language
   - Normalized L2 vectors

3. **Recommendation Query**: `GetMovieRecommendations`
   - CQRS pattern with MediatR
   - Returns similarity-ranked movies
   - Configurable limit (1-50 recommendations)

4. **Embedding Command**: `GenerateMovieEmbeddings`
   - Batch processing for all movies
   - Can regenerate existing embeddings with `force` flag

## API Endpoints

### Get Movie Recommendations

```http
GET /api/movies/{movieId}/recommendations?limit=10
```

**Parameters:**
- `movieId` (Guid): The movie to get recommendations for
- `limit` (int, optional): Number of recommendations (default: 10, max: 50)

**Response:**
```json
{
  "success": true,
  "message": "Success",
  "data": [
    {
      "id": "uuid",
      "tmdbId": 123,
      "title": "Similar Movie",
      "overview": "Movie description...",
      "posterPath": "/path/to/poster.jpg",
      "backdropPath": "/path/to/backdrop.jpg",
      "releaseDate": "2023-01-15T00:00:00",
      "voteAverage": 8.5,
      "popularity": 120.5,
      "similarityScore": 0.85,
      "genres": ["Action", "Thriller"]
    }
  ]
}
```

**Similarity Score**: 0-1 where 1 = identical, 0 = completely different

## Setup Instructions

### 1. Database Migration

The pgvector extension and vector column need to be added to the database. Since EF Core migrations had compatibility issues, here's the manual SQL:

```sql
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Add vector column to Movies table
ALTER TABLE "Movies" 
ADD COLUMN "ContentEmbedding" vector(384);

-- Create index for fast similarity search (optional but recommended for large datasets)
CREATE INDEX ON "Movies" USING ivfflat ("ContentEmbedding" vector_cosine_ops) 
WITH (lists = 100);
```

### 2. Generate Embeddings

After adding the vector column, generate embeddings for existing movies.

**Option A: Via API** (TODO: Add admin endpoint)
```http
POST /api/admin/movies/generate-embeddings
{
  "force": false
}
```

**Option B: Via Code**
```csharp
var command = new GenerateMovieEmbeddingsCommand { Force = false };
var result = await mediator.Send(command);
```

**Option C: Direct Service Call**
```csharp
// In a console app or migration script
var movies = await context.Movies
    .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
    .Include(m => m.MovieKeywords).ThenInclude(mk => mk.Keyword)
    .Where(m => m.ContentEmbedding == null)
    .ToListAsync();

var embeddingService = new MovieEmbeddingService();
foreach (var movie in movies)
{
    movie.ContentEmbedding = embeddingService.GenerateEmbedding(movie);
}
await context.SaveChangesAsync();
```

### 3. Docker Compose Configuration

The PostgreSQL image needs to support pgvector. Update `docker-compose.yml` files:

```yaml
services:
  postgres:
    image: pgvector/pgvector:pg16  # Use pgvector-enabled image
    # ... rest of configuration
```

Or use the ankane/pgvector image:
```yaml
services:
  postgres:
    image: ankane/pgvector:v0.5.1
    # ... rest of configuration
```

## Usage Example

```bash
# 1. Get recommendations for a movie
curl http://localhost:5047/api/movies/{movieId}/recommendations?limit=10

# 2. Get more recommendations
curl http://localhost:5047/api/movies/{movieId}/recommendations?limit=20
```

## Embedding Algorithm

The current implementation uses **feature hashing** - a simple but effective technique:

1. **Feature Extraction**:
   - Title words
   - Overview words (first 50)
   - Genres (weighted 5x)
   - Keywords (top 10, weighted 3x)
   - Language

2. **Hashing**:
   - SHA256 hash for each feature
   - Map to 384 dimensions using modulo
   - Multiple hash functions for better distribution
   - Sign hashing for positive/negative values

3. **Normalization**:
   - L2 normalization (unit vector)
   - Enables cosine similarity comparison

### Future Improvements

For production use with large datasets, consider:

1. **Better Embeddings**:
   - Use pre-trained models (sentence-transformers)
   - BERT, all-MiniLM-L6-v2, etc.
   - Include cast, crew, plot keywords

2. **Hybrid Recommendations**:
   - Combine content-based with collaborative filtering
   - User viewing history
   - Ratings and reviews

3. **Performance**:
   - HNSW index for faster searches
   - Approximate nearest neighbor (ANN)
   - Caching popular recommendations

4. **A/B Testing**:
   - Compare different embedding strategies
   - Measure user engagement

## Testing

Test the recommendation system:

```csharp
// Unit test example
[Fact]
public async Task GetMovieRecommendations_ReturnsSimiIlarMovies()
{
    // Arrange
    var query = new GetMovieRecommendationsQuery
    {
        MovieId = testMovieId,
        Limit = 10
    };

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value.Count);
    Assert.All(result.Value, r => Assert.InRange(r.SimilarityScore, 0, 1));
    Assert.Equal(result.Value, result.Value.OrderByDescending(r => r.SimilarityScore));
}
```

## Troubleshooting

### Embeddings Not Generated
- Ensure movies have genres and keywords loaded
- Check logs for generation errors
- Verify ContentEmbedding column exists

### No Recommendations Returned
- Verify source movie has an embedding
- Check if other movies have embeddings
- Look for database errors in logs

### Slow Performance
- Add IVFFLAT index on ContentEmbedding column
- Consider limiting to movies with min popularity
- Cache popular movie recommendations

## References

- [pgvector Documentation](https://github.com/pgvector/pgvector)
- [pgvector-dotnet](https://github.com/pgvector/pgvector-dotnet)
- [Feature Hashing](https://en.wikipedia.org/wiki/Feature_hashing)
- [Cosine Similarity](https://en.wikipedia.org/wiki/Cosine_similarity)
