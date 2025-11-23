import requests
import psycopg2
from psycopg2.extras import execute_batch
from datetime import datetime, timezone
import time
import logging
import sys
import uuid
import json
import os
import argparse
from typing import Optional, List, Dict, Tuple
from concurrent.futures import ThreadPoolExecutor, as_completed
import threading
from pathlib import Path

# --- Logger Setup ---
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')

# Handler for log file
file_handler = logging.FileHandler('batch_other_values.log', encoding='utf-8')
file_handler.setFormatter(formatter)
logger.addHandler(file_handler)

# Handler for console output
stream_handler = logging.StreamHandler(sys.stdout)
stream_handler.setFormatter(formatter)
logger.addHandler(stream_handler)

# Set console encoding for Windows
if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# Get script directory and project paths
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
INFRASTRUCTURE_DIR = PROJECT_ROOT / "infrastructure"
ENV_FILE = INFRASTRUCTURE_DIR / ".env"
RUN_LOG_PATH = SCRIPT_DIR / "run_log_other_values.json"

# Load .env file
def load_env_file(env_path: Path) -> Dict[str, str]:
    """Load environment variables from .env file"""
    env_vars = {}
    if not env_path.exists():
        logger.error(f"[ENV] .env file not found at: {env_path}")
        raise FileNotFoundError(f".env file not found at: {env_path}")

    with open(env_path, 'r', encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#') and '=' in line:
                key, value = line.split('=', 1)
                env_vars[key.strip()] = value.strip()

    logger.info(f"[ENV] Loaded {len(env_vars)} environment variables")
    return env_vars

# Load environment variables
ENV_VARS = load_env_file(ENV_FILE)

# --- Constants ---
TMDB_ACCESS_TOKEN = ENV_VARS['TMDB_ACCESS_TOKEN']
TMDB_BASE_URL = "https://api.themoviedb.org/3"
HEADERS = {
    "Authorization": f"Bearer {TMDB_ACCESS_TOKEN}",
    "accept": "application/json"
}

def parse_database_url(url: str) -> dict:
    """Parse PostgreSQL URL to connection config"""
    from urllib.parse import urlparse, parse_qs

    parsed = urlparse(url)
    config = {
        'host': parsed.hostname,
        'port': parsed.port or 5432,
        'database': parsed.path.lstrip('/'),
        'user': parsed.username,
        'password': parsed.password,
    }

    # Parse SSL mode from query parameters
    if parsed.query:
        params = parse_qs(parsed.query)
        if 'sslmode' in params:
            config['sslmode'] = params['sslmode'][0]
        else:
            config['sslmode'] = 'require'
    else:
        config['sslmode'] = 'require'

    return config

def get_db_config(db_type: str = 'local') -> dict:
    """Get database configuration based on type (local or neon)"""
    if db_type == 'neon':
        database_url = ENV_VARS.get('DATABASE_URL')
        if not database_url:
            logger.error("DATABASE_URL not found in .env file for Neon database")
            raise ValueError("DATABASE_URL not found in .env file")
        logger.info("[DB] Using Neon PostgreSQL database")
        return parse_database_url(database_url)
    else:
        # Local database configuration
        logger.info("[DB] Using local PostgreSQL database")
        return {
            'host': ENV_VARS.get('DATABASE_HOST', 'localhost'),
            'port': int(ENV_VARS.get('DATABASE_PORT', 5432)),
            'database': ENV_VARS.get('DATABASE_NAME', 'CINE'),
            'user': ENV_VARS.get('DATABASE_USER', 'cinesocial'),
            'password': ENV_VARS.get('DATABASE_PASSWORD', 'cinesocial123'),
            'sslmode': ENV_VARS.get('DATABASE_SSL_MODE', 'disable').lower()
        }

# Global DB_CONFIG will be set in main
DB_CONFIG = None

# --- Database & API Functions ---

def get_db_connection():
    """Establishes a PostgreSQL connection."""
    try:
        return psycopg2.connect(**DB_CONFIG)
    except Exception as e:
        logger.error(f"Database connection error: {e}")
        raise

def load_run_log() -> dict:
    """Loads the run log to resume processing."""
    if os.path.exists(RUN_LOG_PATH):
        try:
            with open(RUN_LOG_PATH, 'r', encoding='utf-8') as f:
                log = json.load(f)
                logger.info(f"Loaded run log. {len(log.get('completed_tmdb_ids', []))} movies already processed.")
                return log
        except (json.JSONDecodeError, IOError) as e:
            logger.error(f"Error loading run log: {e}. Starting fresh.")
    return {"completed_tmdb_ids": []}

def save_run_log(log: dict):
    """Saves the run log."""
    try:
        log['last_update'] = datetime.now(timezone.utc).isoformat()
        with open(RUN_LOG_PATH, 'w', encoding='utf-8') as f:
            json.dump(log, f, indent=2)
    except IOError as e:
        logger.error(f"Error saving run log: {e}")

def get_movies_to_process_from_db(completed_ids: set) -> List[Tuple[str, int]]:
    """Fetches movies from the DB that have not been processed yet."""
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        logger.info("Fetching all movie IDs from the database...")
        cursor.execute('SELECT "Id", "TmdbId" FROM "Movies"')
        all_movies = cursor.fetchall()
        
        movies_to_process = [m for m in all_movies if m[1] not in completed_ids]
        logger.info(f"Found {len(all_movies)} total movies. {len(movies_to_process)} movies need processing.")
        return movies_to_process
    finally:
        cursor.close()
        conn.close()

def fetch_movie_details(movie_id: int) -> Optional[dict]:
    """Fetches detailed movie data from TMDB API."""
    url = f"{TMDB_BASE_URL}/movie/{movie_id}"
    params = {"append_to_response": "videos,images,keywords,credits"}
    try:
        response = requests.get(url, headers=HEADERS, params=params, timeout=15)
        if response.status_code == 200:
            return response.json()
        else:
            logger.error(f"API Error {response.status_code} for movie TMDB ID {movie_id}")
            return None
    except requests.RequestException as e:
        logger.error(f"Request error for movie TMDB ID {movie_id}: {e}")
        return None

def fetch_movie_details_wrapper(internal_uuid: str, tmdb_id: int, sleep_time: float = 0.05) -> Optional[dict]:
    """Thread-safe wrapper for fetching movie details with rate limiting."""
    details = fetch_movie_details(tmdb_id)
    if details:
        details['internal_uuid'] = internal_uuid
    time.sleep(sleep_time)  # Rate limiting
    return details

def batch_save_related_data(movies_data: List[dict]):
    """Saves all related movie data (genres, cast, crew, etc.) to the database."""
    if not movies_data:
        return

    conn = get_db_connection()
    cursor = conn.cursor()

    # Prepare data containers
    genre_values, movie_genre_values = [], []
    company_values, movie_company_values = [], []
    country_values, movie_country_values = [], []
    language_values, movie_language_values = [], []
    keyword_values, movie_keyword_values = [], []
    video_values, image_values = [], []
    person_values, cast_values, crew_values = [], [], []
    collection_values, movie_collection_values = [], []

    try:
        for movie_data in movies_data:
            movie_id = movie_data['internal_uuid'] # This is the crucial internal ID

            # Genres
            for genre in movie_data.get('genres', []):
                genre_values.append((genre['id'], genre['name']))
                movie_genre_values.append((movie_id, genre['id']))

            # Production Companies
            for company in movie_data.get('production_companies', []):
                company_values.append((company['id'], company['name'], company.get('logo_path'), company.get('origin_country')))
                movie_company_values.append((movie_id, company['id']))

            # Cast & Crew (People)
            credits = movie_data.get('credits', {})
            for person in credits.get('cast', [])[:20]: # Top 20 cast
                person_values.append((str(uuid.uuid4()), person['id'], person['name'], person.get('profile_path'), person.get('popularity'), person.get('gender'), person.get('known_for_department')))
                cast_values.append((str(uuid.uuid4()), movie_id, person['id'], person.get('character'), person.get('order')))
            
            for person in credits.get('crew', []):
                if person.get('job') in ['Director', 'Producer', 'Writer', 'Screenplay']:
                    person_values.append((str(uuid.uuid4()), person['id'], person['name'], person.get('profile_path'), person.get('popularity'), person.get('gender'), person.get('known_for_department')))
                    crew_values.append((str(uuid.uuid4()), movie_id, person['id'], person.get('job'), person.get('department')))

            # Keywords
            for keyword in movie_data.get('keywords', {}).get('keywords', []):
                keyword_values.append((keyword['id'], keyword['name']))
                movie_keyword_values.append((movie_id, keyword['id']))

        # --- Batch Inserts ---
        # Note: ON CONFLICT DO NOTHING is used to prevent errors if the entity already exists.
        
        if genre_values:
            execute_batch(cursor, 'INSERT INTO "Genres" ("TmdbId", "Name") VALUES (%s, %s) ON CONFLICT ("TmdbId") DO NOTHING', list(set(genre_values)))
        if movie_genre_values:
            execute_batch(cursor, 'INSERT INTO "MovieGenres" ("MovieId", "GenreId") SELECT %s, "Id" FROM "Genres" WHERE "TmdbId" = %s ON CONFLICT DO NOTHING', movie_genre_values)

        if company_values:
            execute_batch(cursor, 'INSERT INTO "ProductionCompanies" ("TmdbId", "Name", "LogoPath", "OriginCountry") VALUES (%s, %s, %s, %s) ON CONFLICT ("TmdbId") DO NOTHING', list(set(company_values)))
        if movie_company_values:
            execute_batch(cursor, 'INSERT INTO "MovieProductionCompanies" ("MovieId", "ProductionCompanyId") SELECT %s, "Id" FROM "ProductionCompanies" WHERE "TmdbId" = %s ON CONFLICT DO NOTHING', movie_company_values)

        if person_values:
            execute_batch(cursor, 'INSERT INTO "People" ("Id", "TmdbId", "Name", "ProfilePath", "Popularity", "Gender", "KnownForDepartment", "IsDeleted", "CreatedAt") VALUES (%s, %s, %s, %s, %s, %s, %s, FALSE, NOW()) ON CONFLICT ("TmdbId") DO NOTHING', list(set(person_values)))
        if cast_values:
            execute_batch(cursor, 'INSERT INTO "MovieCast" ("Id", "MovieId", "PersonId", "Character", "CastOrder") SELECT %s, %s, "Id", %s, %s FROM "People" WHERE "TmdbId" = %s ON CONFLICT DO NOTHING', [(c[0], c[1], c[3], c[4], c[2]) for c in cast_values])
        if crew_values:
            execute_batch(cursor, 'INSERT INTO "MovieCrew" ("Id", "MovieId", "PersonId", "Job", "Department") SELECT %s, %s, "Id", %s, %s FROM "People" WHERE "TmdbId" = %s ON CONFLICT DO NOTHING', [(c[0], c[1], c[3], c[4], c[2]) for c in crew_values])

        if keyword_values:
            execute_batch(cursor, 'INSERT INTO "Keywords" ("TmdbId", "Name") VALUES (%s, %s) ON CONFLICT ("TmdbId") DO NOTHING', list(set(keyword_values)))
        if movie_keyword_values:
            execute_batch(cursor, 'INSERT INTO "MovieKeywords" ("MovieId", "KeywordId") SELECT %s, "Id" FROM "Keywords" WHERE "TmdbId" = %s ON CONFLICT DO NOTHING', movie_keyword_values)

        conn.commit()
        logger.info(f"Successfully saved related data for a batch of {len(movies_data)} movies.")

    except psycopg2.Error as e:
        conn.rollback()
        logger.error(f"Database error during batch save: {e}")
    except Exception as e:
        conn.rollback()
        logger.error(f"An unexpected error occurred during batch save: {e}")
    finally:
        cursor.close()
        conn.close()

# --- Main Execution ---

def main(batch_size: int, workers: int = 20):
    """Main function to orchestrate fetching and saving related movie data."""
    run_log = load_run_log()
    completed_tmdb_ids = set(run_log.get('completed_tmdb_ids', []))

    movies_to_process = get_movies_to_process_from_db(completed_tmdb_ids)

    if not movies_to_process:
        logger.info("All movies are already up-to-date.")
        return

    total_batches = (len(movies_to_process) + batch_size - 1) // batch_size
    logger.info(f"Starting to process {len(movies_to_process)} movies in {total_batches} batches of size {batch_size}.")
    logger.info(f"Using {workers} parallel workers for faster processing.")

    for i in range(0, len(movies_to_process), batch_size):
        batch = movies_to_process[i:i + batch_size]
        current_batch_num = (i // batch_size) + 1
        logger.info(f"--- Processing Batch {current_batch_num}/{total_batches} ---")

        # Parallel fetching with ThreadPoolExecutor
        detailed_movies_batch = []
        with ThreadPoolExecutor(max_workers=workers) as executor:
            # Submit all tasks for the batch
            future_to_movie = {
                executor.submit(fetch_movie_details_wrapper, internal_uuid, tmdb_id): (internal_uuid, tmdb_id)
                for internal_uuid, tmdb_id in batch
            }

            # Collect results as they complete
            for future in as_completed(future_to_movie):
                internal_uuid, tmdb_id = future_to_movie[future]
                try:
                    details = future.result()
                    if details:
                        detailed_movies_batch.append(details)
                except Exception as e:
                    logger.error(f"Error processing movie TMDB ID {tmdb_id}: {e}")

        logger.info(f"Fetched details for {len(detailed_movies_batch)}/{len(batch)} movies in this batch.")

        if detailed_movies_batch:
            batch_save_related_data(detailed_movies_batch)

        # Update and save the run log after each batch
        for _, tmdb_id in batch:
            completed_tmdb_ids.add(tmdb_id)
        run_log['completed_tmdb_ids'] = list(completed_tmdb_ids)
        save_run_log(run_log)
        logger.info(f"--- Finished Batch {current_batch_num}/{total_batches}. Progress saved. ---")

    logger.info("===================================")
    logger.info("All processing complete!")
    logger.info(f"Total movies updated: {len(completed_tmdb_ids)}")
    logger.info("===================================")


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="Fetch and save related data (cast, crew, genres, etc.) for existing movies in the database.")
    parser.add_argument(
        '--batch-size',
        type=int,
        default=25,
        help='The number of movies to process in each batch. Default is 25.'
    )
    parser.add_argument(
        '--workers',
        type=int,
        default=20,
        help='The number of parallel workers for fetching movie details. Default is 20. Recommended: 10-20 for optimal performance.'
    )
    parser.add_argument(
        '--db',
        type=str,
        choices=['local', 'neon'],
        default='neon',
        help='Database to use: "local" for local PostgreSQL or "neon" for Neon cloud database. Default is neon.'
    )
    args = parser.parse_args()

    # Set global DB_CONFIG based on database type
    DB_CONFIG = get_db_config(args.db)

    main(args.batch_size, args.workers)
