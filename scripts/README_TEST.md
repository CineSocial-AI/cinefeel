# Testing the Recommendation System

This directory contains scripts and tools to test the pgvector-based movie recommendation system.

## Quick Start

### 1. Generate Sample Data

```bash
# From repository root
python3 scripts/generate_sample_movies.py
```

This creates:
- `sample_movies.json` - 20 diverse movies in JSON format
- `sample_movies.sql` - SQL INSERT statements
- `test_scenarios.json` - Expected test outcomes

### 2. Run Full Test Suite

```bash
chmod +x scripts/test_recommendations.sh
./scripts/test_recommendations.sh
```

This script will:
1. Check prerequisites (Docker, PostgreSQL)
2. Insert sample data
3. Verify backend is running
4. Provide next steps for testing

### 3. Manual Testing Steps

If you prefer manual testing:

#### a. Start Services

```bash
# Start PostgreSQL
cd infrastructure
docker-compose -f docker-compose.db.yml up -d
cd ..

# Start Backend API
cd backend/src/CineSocial.Api
dotnet run
```

#### b. Insert Sample Data

```bash
# Option 1: Using psql
psql -h localhost -U cinesocial -d CINE -f scripts/sample_movies.sql

# Option 2: Using docker exec
docker exec -i cinesocial-postgres psql -U cinesocial -d CINE < scripts/sample_movies.sql
```

#### c. Enable pgvector (if not already done)

```sql
-- Connect to database and run:
CREATE EXTENSION IF NOT EXISTS vector;
ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "ContentEmbedding" vector(384);
```

#### d. Generate Embeddings

Option 1: Use the C# test harness (if integrated)
Option 2: Run the command directly:

```csharp
var command = new GenerateMovieEmbeddingsCommand { Force = false };
var result = await mediator.Send(command);
```

Option 3: Direct database query after embedding generation

#### e. Test Recommendations

```bash
# Get list of movies
curl http://localhost:5047/api/movies

# Test with The Matrix (should recommend other sci-fi)
curl http://localhost:5047/api/movies/{matrix-id}/recommendations?limit=5

# Test with Finding Nemo (should recommend animated family films)
curl http://localhost:5047/api/movies/{nemo-id}/recommendations?limit=5

# Test with The Godfather (should recommend crime/mafia films)
curl http://localhost:5047/api/movies/{godfather-id}/recommendations?limit=5
```

## Sample Movies

The generated sample includes 20 diverse movies across different genres:

### Science Fiction & Action
- **The Matrix** (1999) - AI, virtual reality, dystopia
- **Inception** (2010) - Dreams, heist, mind-bending
- **Blade Runner 2049** (2017) - Androids, cyberpunk
- **Interstellar** (2014) - Space exploration, time travel
- **The Avengers** (2012) - Superheroes, alien invasion

### Crime & Drama
- **The Godfather** (1972) - Mafia, family, power
- **Goodfellas** (1990) - Gangster, true story
- **Pulp Fiction** (1994) - Nonlinear, violence
- **The Shawshank Redemption** (1994) - Prison, redemption
- **The Silence of the Lambs** (1991) - Serial killer, FBI

### Animation & Family
- **Finding Nemo** (2003) - Ocean adventure, father-son
- **Toy Story** (1995) - Toys, friendship
- **The Lion King** (1994) - Animals, coming of age
- **Frozen** (2013) - Sisters, magic, Disney

### Thrillers & Others
- **The Dark Knight** (2008) - Batman, Joker, moral dilemmas
- **Ex Machina** (2014) - AI, Turing test
- **Her** (2013) - AI relationship, loneliness
- **Arrival** (2016) - Aliens, language, time perception
- **Jurassic Park** (1993) - Dinosaurs, genetic engineering
- **Mad Max: Fury Road** (2015) - Post-apocalyptic, chase

## Expected Test Results

### Test Case 1: The Matrix
**Expected Similar Movies:**
- Inception (sci-fi, mind-bending concepts)
- Blade Runner 2049 (dystopian AI)
- Interstellar (sci-fi with deep themes)
- Ex Machina (AI and consciousness)

**Reason:** All share science fiction, dystopian, and AI themes

### Test Case 2: Finding Nemo
**Expected Similar Movies:**
- Toy Story (animated, friendship)
- The Lion King (animated, father-son journey)
- Frozen (animated family film)

**Reason:** All are animated family-friendly movies

### Test Case 3: The Godfather
**Expected Similar Movies:**
- Goodfellas (mafia, crime)
- Pulp Fiction (crime drama)
- The Shawshank Redemption (drama with criminal elements)

**Reason:** All are crime dramas with similar themes

### Test Case 4: Ex Machina
**Expected Similar Movies:**
- Her (AI relationship)
- The Matrix (AI consciousness)
- Blade Runner 2049 (androids and identity)

**Reason:** All explore artificial intelligence and consciousness

## Interpreting Results

### Similarity Scores

- **0.9 - 1.0**: Extremely similar (rare, usually same movie)
- **0.7 - 0.9**: Very similar (strong genre/theme overlap)
- **0.5 - 0.7**: Moderately similar (some shared themes)
- **0.3 - 0.5**: Somewhat similar (few shared elements)
- **0.0 - 0.3**: Dissimilar (different genres/themes)

### Success Criteria

A test passes if:
1. At least 2 expected movies appear in top 5 recommendations
2. Similarity scores are > 0.5 for recommended movies
3. Genre/theme alignment makes intuitive sense

## Troubleshooting

### No recommendations returned
- Verify source movie has an embedding (ContentEmbedding column not null)
- Check that other movies have embeddings
- Look for errors in API logs

### Unexpected recommendations
- Review the embedding generation algorithm
- Check if genres and keywords are properly linked
- Verify the cosine similarity calculation

### Database connection errors
- Ensure PostgreSQL is running: `docker ps | grep postgres`
- Check connection string in appsettings
- Verify database credentials

### API errors
- Check backend logs for details
- Verify all migrations are applied
- Ensure pgvector extension is enabled

## Files Generated

| File | Description |
|------|-------------|
| `sample_movies.json` | Movie data in JSON format (for reference) |
| `sample_movies.sql` | SQL INSERT statements (run this) |
| `test_scenarios.json` | Expected test outcomes (for validation) |
| `test_recommendations.sh` | Automated test script |
| `RecommendationSystemTester.cs` | C# test harness class |

## Next Steps After Testing

1. **Production Data**: Replace sample data with real TMDB data
2. **Optimize**: Add IVFFLAT index for faster vector searches
3. **Enhance**: Use better embeddings (sentence transformers)
4. **Monitor**: Track recommendation click-through rates
5. **A/B Test**: Compare different embedding strategies

## Additional Resources

- See `RECOMMENDATIONS.md` in repository root for full documentation
- Check API documentation at `/swagger` when backend is running
- Review embedding algorithm in `MovieEmbeddingService.cs`

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review backend application logs
3. Verify all prerequisites are met
4. Consult RECOMMENDATIONS.md for detailed setup
