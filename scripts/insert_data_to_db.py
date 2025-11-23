import psycopg2
import psycopg2.extras
import csv
import uuid
from datetime import datetime, timezone
import logging
import sys
import os
import argparse
from pathlib import Path

# --- Main Logger Setup ---
logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
main_formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')

# Handler for main log file
file_handler = logging.FileHandler('batch_fetch_v2.log', encoding='utf-8')
file_handler.setFormatter(main_formatter)
logger.addHandler(file_handler)

# Handler for console output
stream_handler = logging.StreamHandler(sys.stdout)
stream_handler.setFormatter(main_formatter)
logger.addHandler(stream_handler)

# --- Error Logger Setup ---
error_logger = logging.getLogger('error_logger')
error_logger.setLevel(logging.ERROR)
error_formatter = logging.Formatter('%(asctime)s - ERROR - %(message)s')

# Handler for error log file
error_file_handler = logging.FileHandler('batch_fetch_errors.log', encoding='utf-8')
error_file_handler.setFormatter(error_formatter)
error_logger.addHandler(error_file_handler)


# Set console encoding for Windows
if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# Get script directory and project paths
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent
DATA_DIR = PROJECT_ROOT / "data"
INFRASTRUCTURE_DIR = PROJECT_ROOT / "infrastructure"
ENV_FILE = INFRASTRUCTURE_DIR / ".env"
CSV_FILE_PATH = DATA_DIR / "TMDB_movie_dataset_v11.csv"

# Load .env file
def load_env_file(env_path: Path):
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

def get_db_connection():
    """PostgreSQL connection"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        return conn
    except Exception as e:
        logger.error(f"Database connection error: {e}")
        raise

def get_existing_tmdb_ids(cursor) -> set:
    """Get all existing TMDB IDs from database"""
    logger.info("Fetching existing movie TMDB IDs from the database...")
    cursor.execute('SELECT "TmdbId" FROM "Movies"')
    existing_ids = {row[0] for row in cursor.fetchall()}
    logger.info(f"Found {len(existing_ids)} existing movies.")
    return existing_ids

def safe_int(value, default=0):
    try:
        return int(float(value))
    except (ValueError, TypeError):
        return default

def safe_float(value, default=0.0):
    try:
        return float(value)
    except (ValueError, TypeError):
        return default

def safe_bool(value, default=False):
    if isinstance(value, str):
        return value.lower() == 'true'
    return bool(value)

def safe_date(value):
    if not value:
        return None
    try:
        # Assuming format YYYY-MM-DD
        return datetime.strptime(value, '%Y-%m-%d').date()
    except ValueError:
        return None

def execute_insert_batch(conn, cursor, movies_to_insert):
    """
    Executes the batch insert for the provided list of movies.
    If the batch fails, it falls back to inserting movies one by one to identify and log the problematic rows.
    """
    if not movies_to_insert:
        return 0

    insert_query = '''
        INSERT INTO "Movies" (
            "Id", "TmdbId", "Title", "OriginalTitle", "Overview", "ReleaseDate",
            "Runtime", "Budget", "Revenue", "PosterPath", "BackdropPath",
            "ImdbId", "OriginalLanguage", "Popularity", "VoteAverage", "VoteCount",
            "Status", "Tagline", "Homepage", "Adult", "IsDeleted", "CreatedAt", "UpdatedAt"
        ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
    '''
    
    try:
        logger.info(f"Attempting to insert a batch of {len(movies_to_insert)} movies...")
        psycopg2.extras.execute_batch(cursor, insert_query, movies_to_insert)
        conn.commit()
        logger.info(f"Successfully inserted batch of {len(movies_to_insert)} movies.")
        return len(movies_to_insert)
    except psycopg2.Error as e:
        conn.rollback()
        logger.warning(f"Batch insert failed: {e}. Switching to single-insert mode for this batch.")
        
        successful_in_batch = 0
        for movie_data in movies_to_insert:
            try:
                cursor.execute(insert_query, movie_data)
                conn.commit()
                successful_in_batch += 1
            except psycopg2.Error as single_e:
                conn.rollback()
                tmdb_id = movie_data[1]
                title = movie_data[2]
                error_logger.error(f"Failed to insert movie: TMDB_ID={tmdb_id}, Title='{title}'. Reason: {single_e}")
                error_logger.error(f"Full data: {movie_data}")

        logger.info(f"Finished processing failed batch: {successful_in_batch} inserted, {len(movies_to_insert) - successful_in_batch} failed and logged.")
        return successful_in_batch

def process_csv_and_insert(batch_size: int):
    """Reads the movie dataset from CSV and inserts new movies into the database in batches."""
    conn = get_db_connection()
    cursor = conn.cursor()

    movies_to_insert = []
    skipped_count = 0
    total_inserted_count = 0
    
    try:
        existing_tmdb_ids = get_existing_tmdb_ids(cursor)
        
        logger.info(f"Starting to process CSV file: {CSV_FILE_PATH} with batch size: {batch_size}")
        
        with open(CSV_FILE_PATH, mode='r', encoding='utf-8') as csvfile:
            reader = csv.DictReader(csvfile)
            for i, row in enumerate(reader, 1):
                tmdb_id = safe_int(row.get('id'))
                if not tmdb_id:
                    logger.warning(f"Skipping row {i} due to missing TMDB ID.")
                    continue

                if tmdb_id in existing_tmdb_ids:
                    skipped_count += 1
                    continue

                movie_values = (
                    str(uuid.uuid4()), tmdb_id, row.get('title'), row.get('original_title'),
                    row.get('overview'), safe_date(row.get('release_date')), safe_int(row.get('runtime')),
                    safe_int(row.get('budget')), safe_int(row.get('revenue')), row.get('poster_path'),
                    row.get('backdrop_path'), row.get('imdb_id'), row.get('original_language'),
                    safe_float(row.get('popularity')), safe_float(row.get('vote_average')),
                    safe_int(row.get('vote_count')), row.get('status'), row.get('tagline'),
                    row.get('homepage'), safe_bool(row.get('adult')), False,
                    datetime.now(timezone.utc), datetime.now(timezone.utc)
                )
                movies_to_insert.append(movie_values)
                existing_tmdb_ids.add(tmdb_id)

                if len(movies_to_insert) >= batch_size:
                    inserted = execute_insert_batch(conn, cursor, movies_to_insert)
                    total_inserted_count += inserted
                    movies_to_insert.clear()

        if movies_to_insert:
            inserted = execute_insert_batch(conn, cursor, movies_to_insert)
            total_inserted_count += inserted

        logger.info("--------------------------------------------------")
        logger.info("CSV Processing Complete!")
        logger.info(f"Skipped {skipped_count} movies that already exist in the database.")
        logger.info(f"[SUCCESS] Inserted a total of {total_inserted_count} new movies.")
        logger.warning("""
        NOTE: Only data for the main 'Movies' table was inserted. 
        Relational data such as genres, keywords, production companies, etc., was NOT inserted.
        This is because the source CSV file contains only names for these entities (e.g., 'Action'), 
        but the database schema requires unique IDs (e.g., TMDB Genre ID) which are not present in the CSV.
        To populate this related data, a more complex script would be needed to either fetch the correct IDs from an API 
        or modify the database schema.
        """)

    except FileNotFoundError:
        logger.error(f"CSV file not found at path: {CSV_FILE_PATH}")
    except Exception as e:
        conn.rollback()
        logger.error(f"A critical error occurred: {e}")
    finally:
        cursor.close()
        conn.close()
        logger.info("Database connection closed.")


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="Batch insert movies from a CSV file into a PostgreSQL database.")
    parser.add_argument(
        '--batch-size',
        type=int,
        default=50,
        help='The number of movies to insert in each batch. Default is 50.'
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

    process_csv_and_insert(args.batch_size)
