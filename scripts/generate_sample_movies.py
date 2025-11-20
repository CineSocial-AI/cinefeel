#!/usr/bin/env python3
"""
Sample movie data generator and recommendation system tester for CineFeel.
Creates 20 diverse movies and tests the pgvector-based recommendation system.
"""

import json
import uuid
from datetime import datetime, timedelta
import random

# Sample movie data with diverse genres, keywords, and themes
SAMPLE_MOVIES = [
    {
        "title": "The Matrix",
        "overview": "A computer hacker learns about the true nature of reality and his role in the war against its controllers.",
        "genres": ["Science Fiction", "Action"],
        "keywords": ["dystopia", "artificial intelligence", "virtual reality", "chosen one", "martial arts"],
        "release_date": "1999-03-31",
        "vote_average": 8.7,
        "popularity": 85.4
    },
    {
        "title": "Inception",
        "overview": "A thief who steals corporate secrets through dream-sharing technology is given the inverse task of planting an idea.",
        "genres": ["Science Fiction", "Action", "Thriller"],
        "keywords": ["dream", "heist", "subconscious", "time dilation", "mind bending"],
        "release_date": "2010-07-16",
        "vote_average": 8.8,
        "popularity": 92.1
    },
    {
        "title": "The Shawshank Redemption",
        "overview": "Two imprisoned men bond over years, finding solace and eventual redemption through acts of common decency.",
        "genres": ["Drama", "Crime"],
        "keywords": ["prison", "friendship", "hope", "escape", "redemption"],
        "release_date": "1994-09-23",
        "vote_average": 9.3,
        "popularity": 88.7
    },
    {
        "title": "Interstellar",
        "overview": "A team of explorers travel through a wormhole in space in an attempt to ensure humanity's survival.",
        "genres": ["Science Fiction", "Drama", "Adventure"],
        "keywords": ["space", "wormhole", "time travel", "father daughter", "black hole"],
        "release_date": "2014-11-07",
        "vote_average": 8.6,
        "popularity": 81.3
    },
    {
        "title": "The Dark Knight",
        "overview": "Batman must accept one of the greatest psychological and physical tests to fight injustice and chaos.",
        "genres": ["Action", "Crime", "Drama"],
        "keywords": ["superhero", "joker", "vigilante", "chaos", "moral dilemma"],
        "release_date": "2008-07-18",
        "vote_average": 9.0,
        "popularity": 95.2
    },
    {
        "title": "Blade Runner 2049",
        "overview": "A young blade runner's discovery leads him on a quest to find a former blade runner who's been missing.",
        "genres": ["Science Fiction", "Thriller"],
        "keywords": ["dystopia", "android", "artificial intelligence", "cyberpunk", "identity"],
        "release_date": "2017-10-06",
        "vote_average": 8.0,
        "popularity": 73.5
    },
    {
        "title": "The Godfather",
        "overview": "The aging patriarch of an organized crime dynasty transfers control of his empire to his reluctant son.",
        "genres": ["Drama", "Crime"],
        "keywords": ["mafia", "family", "power", "betrayal", "Italian"],
        "release_date": "1972-03-24",
        "vote_average": 9.2,
        "popularity": 87.9
    },
    {
        "title": "Pulp Fiction",
        "overview": "The lives of two mob hitmen, a boxer, and a pair of diner bandits intertwine in four tales of violence.",
        "genres": ["Crime", "Drama", "Thriller"],
        "keywords": ["nonlinear", "violence", "redemption", "dialogue", "hitman"],
        "release_date": "1994-10-14",
        "vote_average": 8.9,
        "popularity": 84.6
    },
    {
        "title": "Finding Nemo",
        "overview": "After his son is captured, a clownfish embarks on a journey to rescue him with a forgetful companion.",
        "genres": ["Animation", "Family", "Adventure"],
        "keywords": ["ocean", "fish", "father son", "journey", "disability"],
        "release_date": "2003-05-30",
        "vote_average": 8.1,
        "popularity": 76.8
    },
    {
        "title": "The Avengers",
        "overview": "Earth's mightiest heroes must come together to stop an alien invasion threatening the planet.",
        "genres": ["Action", "Science Fiction", "Adventure"],
        "keywords": ["superhero", "team", "alien invasion", "marvel", "epic battle"],
        "release_date": "2012-05-04",
        "vote_average": 7.9,
        "popularity": 89.4
    },
    {
        "title": "Toy Story",
        "overview": "A cowboy doll is threatened when a new spaceman action figure supplants him as top toy in a boy's room.",
        "genres": ["Animation", "Family", "Comedy"],
        "keywords": ["toys", "jealousy", "friendship", "childhood", "adventure"],
        "release_date": "1995-11-22",
        "vote_average": 8.3,
        "popularity": 82.1
    },
    {
        "title": "Goodfellas",
        "overview": "The story of Henry Hill and his life in the mob, covering his relationship with his wife and partners.",
        "genres": ["Crime", "Drama"],
        "keywords": ["mafia", "gangster", "true story", "drugs", "betrayal"],
        "release_date": "1990-09-19",
        "vote_average": 8.7,
        "popularity": 79.3
    },
    {
        "title": "Frozen",
        "overview": "When a kingdom is trapped in eternal winter, Anna teams up with a mountain man to find her sister Elsa.",
        "genres": ["Animation", "Family", "Fantasy"],
        "keywords": ["sisters", "ice", "magic", "disney", "musical"],
        "release_date": "2013-11-27",
        "vote_average": 7.4,
        "popularity": 91.7
    },
    {
        "title": "Arrival",
        "overview": "A linguist works with the military to communicate with aliens after mysterious spacecraft appear worldwide.",
        "genres": ["Science Fiction", "Drama"],
        "keywords": ["alien", "language", "time perception", "communication", "first contact"],
        "release_date": "2016-11-11",
        "vote_average": 7.9,
        "popularity": 71.2
    },
    {
        "title": "The Lion King",
        "overview": "Lion prince Simba flees his kingdom after his father's death, only to learn the true meaning of responsibility.",
        "genres": ["Animation", "Family", "Drama"],
        "keywords": ["animals", "africa", "coming of age", "revenge", "musical"],
        "release_date": "1994-06-24",
        "vote_average": 8.5,
        "popularity": 93.8
    },
    {
        "title": "Ex Machina",
        "overview": "A programmer is invited to administer the Turing test to an intelligent humanoid robot.",
        "genres": ["Science Fiction", "Drama", "Thriller"],
        "keywords": ["artificial intelligence", "turing test", "consciousness", "manipulation", "android"],
        "release_date": "2014-04-10",
        "vote_average": 7.6,
        "popularity": 68.9
    },
    {
        "title": "The Silence of the Lambs",
        "overview": "A young FBI cadet seeks the advice of an imprisoned cannibal to catch another serial killer.",
        "genres": ["Crime", "Drama", "Thriller"],
        "keywords": ["serial killer", "fbi", "psychological", "cat and mouse", "manipulation"],
        "release_date": "1991-02-14",
        "vote_average": 8.6,
        "popularity": 77.4
    },
    {
        "title": "Jurassic Park",
        "overview": "A pragmatic paleontologist visiting a theme park fights for survival when power fails and dinosaurs escape.",
        "genres": ["Science Fiction", "Adventure", "Thriller"],
        "keywords": ["dinosaurs", "genetic engineering", "theme park", "survival", "chaos theory"],
        "release_date": "1993-06-11",
        "vote_average": 8.1,
        "popularity": 86.5
    },
    {
        "title": "Her",
        "overview": "In a near future, a lonely writer develops an unlikely relationship with an operating system.",
        "genres": ["Science Fiction", "Drama", "Romance"],
        "keywords": ["artificial intelligence", "loneliness", "technology", "voice", "future"],
        "release_date": "2013-12-18",
        "vote_average": 7.9,
        "popularity": 69.7
    },
    {
        "title": "Mad Max: Fury Road",
        "overview": "In a post-apocalyptic wasteland, a woman rebels against a tyrannical ruler in search of her homeland.",
        "genres": ["Action", "Science Fiction", "Adventure"],
        "keywords": ["post apocalyptic", "desert", "chase", "survival", "feminism"],
        "release_date": "2015-05-15",
        "vote_average": 7.9,
        "popularity": 75.6
    }
]

def generate_movies_json():
    """Generate a JSON file with sample movie data."""
    movies = []
    for i, movie_data in enumerate(SAMPLE_MOVIES, 1):
        movie = {
            "id": str(uuid.uuid4()),
            "tmdbId": 1000 + i,  # Fake TMDB IDs
            "title": movie_data["title"],
            "originalTitle": movie_data["title"],
            "overview": movie_data["overview"],
            "releaseDate": movie_data["release_date"],
            "runtime": random.randint(90, 180),
            "budget": random.randint(10000000, 200000000),
            "revenue": random.randint(50000000, 2000000000),
            "posterPath": f"/poster_{i}.jpg",
            "backdropPath": f"/backdrop_{i}.jpg",
            "originalLanguage": "en",
            "popularity": movie_data["popularity"],
            "voteAverage": movie_data["vote_average"],
            "voteCount": random.randint(1000, 50000),
            "status": "Released",
            "adult": False,
            "genres": movie_data["genres"],
            "keywords": movie_data["keywords"]
        }
        movies.append(movie)
    
    return movies

def generate_sql_insert():
    """Generate SQL INSERT statements for the sample movies."""
    movies = generate_movies_json()
    
    sql_statements = []
    sql_statements.append("-- Sample Movies for Testing CineFeel Recommendation System\n")
    sql_statements.append("-- Generated on: " + datetime.now().isoformat() + "\n\n")
    
    # First, let's create genres
    all_genres = set()
    for movie in movies:
        all_genres.update(movie['genres'])
    
    sql_statements.append("-- Insert Genres\n")
    for i, genre in enumerate(sorted(all_genres), 1):
        sql_statements.append(
            f"INSERT INTO \"Genres\" (\"Id\", \"TmdbId\", \"Name\", \"CreatedAt\", \"UpdatedAt\") "
            f"VALUES ('{uuid.uuid4()}', {500 + i}, '{genre}', NOW(), NOW()) "
            f"ON CONFLICT (\"TmdbId\") DO NOTHING;\n"
        )
    
    sql_statements.append("\n-- Insert Keywords\n")
    all_keywords = set()
    for movie in movies:
        all_keywords.update(movie['keywords'])
    
    for i, keyword in enumerate(sorted(all_keywords), 1):
        sql_statements.append(
            f"INSERT INTO \"Keywords\" (\"Id\", \"TmdbId\", \"Name\", \"CreatedAt\", \"UpdatedAt\") "
            f"VALUES ('{uuid.uuid4()}', {1000 + i}, '{keyword}', NOW(), NOW()) "
            f"ON CONFLICT (\"TmdbId\") DO NOTHING;\n"
        )
    
    sql_statements.append("\n-- Insert Movies\n")
    for movie in movies:
        title_escaped = movie['title'].replace("'", "''")
        overview_escaped = movie['overview'].replace("'", "''")
        sql_statements.append(
            f"INSERT INTO \"Movies\" ("
            f"\"Id\", \"TmdbId\", \"Title\", \"OriginalTitle\", \"Overview\", \"ReleaseDate\", "
            f"\"Runtime\", \"Budget\", \"Revenue\", \"PosterPath\", \"BackdropPath\", "
            f"\"OriginalLanguage\", \"Popularity\", \"VoteAverage\", \"VoteCount\", "
            f"\"Status\", \"Adult\", \"CreatedAt\", \"UpdatedAt\""
            f") VALUES ("
            f"'{movie['id']}', {movie['tmdbId']}, '{title_escaped}', "
            f"'{title_escaped}', '{overview_escaped}', "
            f"'{movie['releaseDate']}', {movie['runtime']}, {movie['budget']}, {movie['revenue']}, "
            f"'{movie['posterPath']}', '{movie['backdropPath']}', '{movie['originalLanguage']}', "
            f"{movie['popularity']}, {movie['voteAverage']}, {movie['voteCount']}, "
            f"'{movie['status']}', {str(movie['adult']).lower()}, NOW(), NOW()"
            f") ON CONFLICT (\"TmdbId\") DO NOTHING;\n"
        )
    
    sql_statements.append("\n-- Link Movies to Genres\n")
    for movie in movies:
        for genre in movie['genres']:
            sql_statements.append(
                f"INSERT INTO \"MovieGenres\" (\"Id\", \"MovieId\", \"GenreId\", \"CreatedAt\", \"UpdatedAt\") "
                f"SELECT '{uuid.uuid4()}', "
                f"(SELECT \"Id\" FROM \"Movies\" WHERE \"TmdbId\" = {movie['tmdbId']}), "
                f"(SELECT \"Id\" FROM \"Genres\" WHERE \"Name\" = '{genre}'), "
                f"NOW(), NOW() "
                f"WHERE NOT EXISTS (SELECT 1 FROM \"MovieGenres\" mg "
                f"WHERE mg.\"MovieId\" = (SELECT \"Id\" FROM \"Movies\" WHERE \"TmdbId\" = {movie['tmdbId']}) "
                f"AND mg.\"GenreId\" = (SELECT \"Id\" FROM \"Genres\" WHERE \"Name\" = '{genre}'));\n"
            )
    
    sql_statements.append("\n-- Link Movies to Keywords\n")
    for movie in movies:
        for keyword in movie['keywords']:
            sql_statements.append(
                f"INSERT INTO \"MovieKeywords\" (\"Id\", \"MovieId\", \"KeywordId\", \"CreatedAt\", \"UpdatedAt\") "
                f"SELECT '{uuid.uuid4()}', "
                f"(SELECT \"Id\" FROM \"Movies\" WHERE \"TmdbId\" = {movie['tmdbId']}), "
                f"(SELECT \"Id\" FROM \"Keywords\" WHERE \"Name\" = '{keyword}'), "
                f"NOW(), NOW() "
                f"WHERE NOT EXISTS (SELECT 1 FROM \"MovieKeywords\" mk "
                f"WHERE mk.\"MovieId\" = (SELECT \"Id\" FROM \"Movies\" WHERE \"TmdbId\" = {movie['tmdbId']}) "
                f"AND mk.\"KeywordId\" = (SELECT \"Id\" FROM \"Keywords\" WHERE \"Name\" = '{keyword}'));\n"
            )
    
    return ''.join(sql_statements)

def generate_test_scenarios():
    """Generate test scenarios for the recommendation system."""
    movies = generate_movies_json()
    
    test_cases = []
    
    # Test case 1: Science Fiction movies should recommend other sci-fi
    test_cases.append({
        "movie": "The Matrix",
        "expected_similar": ["Inception", "Blade Runner 2049", "Interstellar", "Ex Machina"],
        "reason": "All are science fiction with dystopian/AI themes"
    })
    
    # Test case 2: Animated family movies should cluster together
    test_cases.append({
        "movie": "Finding Nemo",
        "expected_similar": ["Toy Story", "The Lion King", "Frozen"],
        "reason": "All are animated family-friendly movies"
    })
    
    # Test case 3: Crime dramas should recommend similar
    test_cases.append({
        "movie": "The Godfather",
        "expected_similar": ["Goodfellas", "Pulp Fiction", "The Shawshank Redemption"],
        "reason": "All are crime/drama films"
    })
    
    # Test case 4: AI-themed movies
    test_cases.append({
        "movie": "Ex Machina",
        "expected_similar": ["Her", "The Matrix", "Blade Runner 2049"],
        "reason": "All explore artificial intelligence and consciousness"
    })
    
    return test_cases

if __name__ == "__main__":
    # Generate JSON
    movies_json = generate_movies_json()
    with open('sample_movies.json', 'w') as f:
        json.dump(movies_json, f, indent=2)
    print(f"✓ Generated sample_movies.json with {len(movies_json)} movies")
    
    # Generate SQL
    sql_content = generate_sql_insert()
    with open('sample_movies.sql', 'w') as f:
        f.write(sql_content)
    print(f"✓ Generated sample_movies.sql with INSERT statements")
    
    # Generate test scenarios
    test_scenarios = generate_test_scenarios()
    with open('test_scenarios.json', 'w') as f:
        json.dump(test_scenarios, f, indent=2)
    print(f"✓ Generated test_scenarios.json with {len(test_scenarios)} test cases")
    
    print("\n" + "="*60)
    print("Sample Movies Generated Successfully!")
    print("="*60)
    print("\nNext steps:")
    print("1. Run the SQL file to insert sample data:")
    print("   psql -h localhost -U cinesocial -d CINE -f sample_movies.sql")
    print("\n2. Generate embeddings (run in backend):")
    print("   Use GenerateMovieEmbeddingsCommand")
    print("\n3. Test recommendations:")
    print("   curl http://localhost:5047/api/movies/{movieId}/recommendations")
    print("\nFiles created:")
    print("- sample_movies.json: Movie data in JSON format")
    print("- sample_movies.sql: SQL INSERT statements")
    print("- test_scenarios.json: Expected test outcomes")
