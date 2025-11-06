import requests
import psycopg2
from psycopg2.extras import execute_batch
from datetime import datetime, timezone
import time
import logging
import sys
import uuid
import os
from typing import Optional, List, Dict
from concurrent.futures import ThreadPoolExecutor, as_completed
import threading
from dotenv import load_dotenv

# Load environment variables from infrastructure/.env
env_path = os.path.join(os.path.dirname(__file__), 'infrastructure', '.env')
load_dotenv(env_path)

# Logging setup
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('batch_fetch_movies.log', encoding='utf-8'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)

# Set console encoding for Windows
if sys.platform == 'win32':
    sys.stdout.reconfigure(encoding='utf-8')

# TMDB API Settings
TMDB_ACCESS_TOKEN = os.getenv('TMDB_ACCESS_TOKEN')
if not TMDB_ACCESS_TOKEN:
    raise ValueError("TMDB_ACCESS_TOKEN not found in environment variables. Please set it in .env file.")

TMDB_BASE_URL = "https://api.themoviedb.org/3"
HEADERS = {
    "Authorization": f"Bearer {TMDB_ACCESS_TOKEN}",
    "accept": "application/json"
}

# PostgreSQL Connection String
DB_CONFIG = {
    'host': os.getenv('DATABASE_HOST', 'localhost'),
    'port': int(os.getenv('DATABASE_PORT', 5432)),
    'database': os.getenv('DATABASE_NAME', 'CINE'),
    'user': os.getenv('DATABASE_USER', 'cinesocial'),
    'password': os.getenv('DATABASE_PASSWORD', 'cinesocial123'),
    'sslmode': os.getenv('DATABASE_SSL_MODE', 'disable').lower()
}

# Thread-safe rate limiting
rate_limit_lock = threading.Lock()
last_request_time = [0]  # Use list to make it mutable

def rate_limited_sleep(delay: float = 0.05):
    """Thread-safe rate limiting"""
    with rate_limit_lock:
        current_time = time.time()
        time_since_last = current_time - last_request_time[0]
        if time_since_last < delay:
            time.sleep(delay - time_since_last)
        last_request_time[0] = time.time()

def get_db_connection():
    """PostgreSQL connection"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        return conn
    except Exception as e:
        logger.error(f"Database connection error: {e}")
        raise

def fetch_movie_details(movie_id: int) -> Optional[dict]:
    """Fetch movie details from TMDB API with rate limiting"""
    url = f"{TMDB_BASE_URL}/movie/{movie_id}"
    params = {
        "append_to_response": "videos,images,keywords,credits"
    }

    rate_limited_sleep(0.05)  # 20 requests per second max

    try:
        response = requests.get(url, headers=HEADERS, params=params, timeout=10)
        if response.status_code == 200:
            return response.json()
        elif response.status_code == 404:
            logger.warning(f"Movie {movie_id} not found (404)")
            return None
        else:
            logger.error(f"API Error {response.status_code} for movie {movie_id}")
            return None
    except Exception as e:
        logger.error(f"Request error for movie {movie_id}: {e}")
        return None

def fetch_movies_parallel(movie_ids: List[int], max_workers: int = 10) -> List[dict]:
    """Fetch multiple movie details in parallel"""
    detailed_movies = []
    
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        future_to_id = {executor.submit(fetch_movie_details, movie_id): movie_id 
                       for movie_id in movie_ids}
        
        for future in as_completed(future_to_id):
            movie_id = future_to_id[future]
            try:
                result = future.result()
                if result:
                    detailed_movies.append(result)
            except Exception as e:
                logger.error(f"Error fetching movie {movie_id}: {e}")
    
    return detailed_movies

def get_existing_tmdb_ids(cursor) -> set:
    """Get all existing TMDB IDs from database"""
    cursor.execute('SELECT "TmdbId" FROM "Movies"')
    return {row[0] for row in cursor.fetchall()}

def batch_save_movies(movies_data: List[dict]) -> tuple:
    """Save multiple movies in batch with all related data"""
    if not movies_data:
        return (0, 0, 0)
    
    conn = get_db_connection()
    cursor = conn.cursor()

    saved_count = 0
    skipped_count = 0
    failed_count = 0

    try:
        # Get existing movie IDs
        existing_ids = get_existing_tmdb_ids(cursor)

        # Filter out existing movies
        new_movies = [m for m in movies_data if m.get('id') not in existing_ids]
        skipped_count = len(movies_data) - len(new_movies)

        if not new_movies:
            logger.info(f"[SKIP] All {len(movies_data)} movies already exist")
            conn.close()
            return (0, skipped_count, 0)

        logger.info(f"[BATCH] Processing {len(new_movies)} new movies (skipping {skipped_count})")

        # Prepare movie insert data
        movie_values = []
        movie_id_map = {}  # TMDB ID -> Local ID mapping

        for movie_data in new_movies:
            movie_values.append((
                str(uuid.uuid4()),  # Id (Guid)
                movie_data.get('id'),
                movie_data.get('title'),
                movie_data.get('original_title'),
                movie_data.get('overview'),
                movie_data.get('release_date'),
                movie_data.get('runtime'),
                movie_data.get('budget'),
                movie_data.get('revenue'),
                movie_data.get('poster_path'),
                movie_data.get('backdrop_path'),
                movie_data.get('imdb_id'),
                movie_data.get('original_language'),
                movie_data.get('popularity'),
                movie_data.get('vote_average'),
                movie_data.get('vote_count'),
                movie_data.get('status'),
                movie_data.get('tagline'),
                movie_data.get('homepage'),
                movie_data.get('adult', False),
                False,  # IsDeleted
                datetime.now(timezone.utc),
                datetime.now(timezone.utc)
            ))

        # Batch insert movies
        execute_batch(cursor, '''
            INSERT INTO "Movies" (
                "Id", "TmdbId", "Title", "OriginalTitle", "Overview", "ReleaseDate",
                "Runtime", "Budget", "Revenue", "PosterPath", "BackdropPath",
                "ImdbId", "OriginalLanguage", "Popularity", "VoteAverage", "VoteCount",
                "Status", "Tagline", "Homepage", "Adult", "IsDeleted", "CreatedAt", "UpdatedAt"
            ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
        ''', movie_values)

        # Get inserted movie IDs
        tmdb_ids = [m.get('id') for m in new_movies]
        cursor.execute(
            'SELECT "Id", "TmdbId" FROM "Movies" WHERE "TmdbId" = ANY(%s)',
            (tmdb_ids,)
        )
        for row in cursor.fetchall():
            movie_id_map[row[1]] = row[0]

        # Process related data for each movie
        genre_values = []
        movie_genre_values = []
        company_values = []
        movie_company_values = []
        country_values = []
        movie_country_values = []
        language_values = []
        movie_language_values = []
        keyword_values = []
        movie_keyword_values = []
        video_values = []
        image_values = []
        person_values = []
        cast_values = []
        crew_values = []
        collection_values = []
        movie_collection_values = []

        for movie_data in new_movies:
            movie_id = movie_id_map.get(movie_data.get('id'))
            if not movie_id:
                continue

            # Genres
            if 'genres' in movie_data:
                for genre in movie_data['genres']:
                    genre_values.append((genre['id'], genre['name']))
                    movie_genre_values.append((movie_id, genre['id']))

            # Production Companies
            if 'production_companies' in movie_data:
                for company in movie_data['production_companies']:
                    company_values.append((
                        company['id'],
                        company['name'],
                        company.get('logo_path'),
                        company.get('origin_country')
                    ))
                    movie_company_values.append((movie_id, company['id']))

            # Countries
            if 'production_countries' in movie_data:
                for country in movie_data['production_countries']:
                    country_values.append((country['iso_3166_1'], country['name']))
                    movie_country_values.append((movie_id, country['iso_3166_1']))

            # Languages
            if 'spoken_languages' in movie_data:
                for language in movie_data['spoken_languages']:
                    language_values.append((
                        language['iso_639_1'],
                        language['name'],
                        language.get('english_name')
                    ))
                    movie_language_values.append((movie_id, language['iso_639_1']))

            # Keywords
            if 'keywords' in movie_data and 'keywords' in movie_data['keywords']:
                for keyword in movie_data['keywords']['keywords']:
                    keyword_values.append((keyword['id'], keyword['name']))
                    movie_keyword_values.append((movie_id, keyword['id']))

            # Videos
            if 'videos' in movie_data and 'results' in movie_data['videos']:
                for video in movie_data['videos']['results']:
                    video_values.append((
                        str(uuid.uuid4()),  # Id
                        movie_id,
                        video['key'],
                        video.get('name'),
                        video.get('site'),
                        video.get('type'),
                        video.get('official', False)
                    ))

            # Images
            if 'images' in movie_data:
                if 'posters' in movie_data['images']:
                    for image in movie_data['images']['posters'][:5]:
                        image_values.append((
                            str(uuid.uuid4()),  # Id
                            movie_id, image['file_path'], 'poster',
                            image.get('iso_639_1'), image.get('vote_average'),
                            image.get('vote_count'), image.get('width'), image.get('height')
                        ))
                if 'backdrops' in movie_data['images']:
                    for image in movie_data['images']['backdrops'][:5]:
                        image_values.append((
                            str(uuid.uuid4()),  # Id
                            movie_id, image['file_path'], 'backdrop',
                            image.get('iso_639_1'), image.get('vote_average'),
                            image.get('vote_count'), image.get('width'), image.get('height')
                        ))

            # Cast & Crew
            if 'credits' in movie_data:
                if 'cast' in movie_data['credits']:
                    for person in movie_data['credits']['cast'][:10]:
                        person_values.append((
                            str(uuid.uuid4()),  # Id
                            person['id'], person['name'], person.get('profile_path'),
                            person.get('popularity'), person.get('gender'),
                            person.get('known_for_department'), False, datetime.now(timezone.utc)
                        ))
                        cast_values.append((
                            str(uuid.uuid4()),  # Id
                            movie_id, person['id'],  # TmdbId for lookup
                            person.get('character'), person.get('order')
                        ))

                if 'crew' in movie_data['credits']:
                    for person in movie_data['credits']['crew']:
                        if person.get('job') in ['Director', 'Producer', 'Writer', 'Screenplay']:
                            person_values.append((
                                str(uuid.uuid4()),  # Id
                                person['id'], person['name'], person.get('profile_path'),
                                person.get('popularity'), person.get('gender'),
                                person.get('known_for_department'), False, datetime.now(timezone.utc)
                            ))
                            crew_values.append((
                                str(uuid.uuid4()),  # Id
                                movie_id, person['id'],  # TmdbId for lookup
                                person.get('job'), person.get('department')
                            ))

            # Collection
            if movie_data.get('belongs_to_collection'):
                collection = movie_data['belongs_to_collection']
                collection_values.append((
                    collection['id'], collection['name'],
                    collection.get('poster_path'), collection.get('backdrop_path')
                ))
                movie_collection_values.append((movie_id, collection['id']))

        # Batch insert all related data
        if genre_values:
            execute_batch(cursor, '''
                INSERT INTO "Genres" ("TmdbId", "Name")
                VALUES (%s, %s)
                ON CONFLICT ("TmdbId") DO NOTHING
            ''', list(set(genre_values)))

        if movie_genre_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieGenres" ("MovieId", "GenreId")
                SELECT %s, "Id" FROM "Genres" WHERE "TmdbId" = %s
                ON CONFLICT DO NOTHING
            ''', movie_genre_values)

        if company_values:
            execute_batch(cursor, '''
                INSERT INTO "ProductionCompanies" ("TmdbId", "Name", "LogoPath", "OriginCountry")
                VALUES (%s, %s, %s, %s)
                ON CONFLICT ("TmdbId") DO NOTHING
            ''', list(set(company_values)))

        if movie_company_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieProductionCompanies" ("MovieId", "ProductionCompanyId")
                SELECT %s, "Id" FROM "ProductionCompanies" WHERE "TmdbId" = %s
                ON CONFLICT DO NOTHING
            ''', movie_company_values)

        if country_values:
            execute_batch(cursor, '''
                INSERT INTO "Countries" ("Iso31661", "Name")
                VALUES (%s, %s)
                ON CONFLICT ("Iso31661") DO NOTHING
            ''', list(set(country_values)))

        if movie_country_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieCountries" ("MovieId", "CountryId")
                SELECT %s, "Id" FROM "Countries" WHERE "Iso31661" = %s
                ON CONFLICT DO NOTHING
            ''', movie_country_values)

        if language_values:
            execute_batch(cursor, '''
                INSERT INTO "Languages" ("Iso6391", "Name", "EnglishName")
                VALUES (%s, %s, %s)
                ON CONFLICT ("Iso6391") DO NOTHING
            ''', list(set(language_values)))

        if movie_language_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieLanguages" ("MovieId", "LanguageId")
                SELECT %s, "Id" FROM "Languages" WHERE "Iso6391" = %s
                ON CONFLICT DO NOTHING
            ''', movie_language_values)

        if keyword_values:
            execute_batch(cursor, '''
                INSERT INTO "Keywords" ("TmdbId", "Name")
                VALUES (%s, %s)
                ON CONFLICT ("TmdbId") DO NOTHING
            ''', list(set(keyword_values)))

        if movie_keyword_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieKeywords" ("MovieId", "KeywordId")
                SELECT %s, "Id" FROM "Keywords" WHERE "TmdbId" = %s
                ON CONFLICT DO NOTHING
            ''', movie_keyword_values)

        if video_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieVideos" ("Id", "MovieId", "VideoKey", "Name", "Site", "Type", "Official")
                VALUES (%s, %s, %s, %s, %s, %s, %s)
            ''', video_values)

        if image_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieImages" (
                    "Id", "MovieId", "FilePath", "ImageType", "Language",
                    "VoteAverage", "VoteCount", "Width", "Height"
                ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
            ''', image_values)

        if person_values:
            execute_batch(cursor, '''
                INSERT INTO "People" (
                    "Id", "TmdbId", "Name", "ProfilePath", "Popularity",
                    "Gender", "KnownForDepartment", "IsDeleted", "CreatedAt"
                ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
                ON CONFLICT ("TmdbId") DO NOTHING
            ''', list(set(person_values)))

        if cast_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieCast" ("Id", "MovieId", "PersonId", "Character", "CastOrder")
                SELECT %s, %s, "Id", %s, %s FROM "People" WHERE "TmdbId" = %s
            ''', [(c[0], c[1], c[3], c[4], c[2]) for c in cast_values])

        if crew_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieCrew" ("Id", "MovieId", "PersonId", "Job", "Department")
                SELECT %s, %s, "Id", %s, %s FROM "People" WHERE "TmdbId" = %s
            ''', [(c[0], c[1], c[3], c[4], c[2]) for c in crew_values])

        if collection_values:
            execute_batch(cursor, '''
                INSERT INTO "Collections" ("TmdbId", "Name", "PosterPath", "BackdropPath")
                VALUES (%s, %s, %s, %s)
                ON CONFLICT ("TmdbId") DO NOTHING
            ''', list(set(collection_values)))

        if movie_collection_values:
            execute_batch(cursor, '''
                INSERT INTO "MovieCollections" ("MovieId", "CollectionId")
                SELECT %s, "Id" FROM "Collections" WHERE "TmdbId" = %s
                ON CONFLICT DO NOTHING
            ''', movie_collection_values)

        conn.commit()
        saved_count = len(new_movies)
        logger.info(f"[BATCH-OK] Successfully saved {saved_count} movies with all related data")

    except Exception as e:
        conn.rollback()
        logger.error(f"[BATCH-ERROR] Error in batch insert: {e}")
        failed_count = len(movies_data) - skipped_count
        saved_count = 0

    finally:
        cursor.close()
        conn.close()

    return (saved_count, skipped_count, failed_count)

def fetch_page_movies(page: int) -> tuple:
    """Fetch and process a single page of movies"""
    logger.info(f"[PAGE] Fetching page {page}...")
    
    url = f"{TMDB_BASE_URL}/movie/popular"
    params = {"page": page}

    try:
        rate_limited_sleep(0.1)
        response = requests.get(url, headers=HEADERS, params=params, timeout=10)
        if response.status_code != 200:
            logger.error(f"Failed to fetch page {page}: {response.status_code}")
            return (0, 0, 0)

        data = response.json()
        movies = data.get('results', [])
        logger.info(f"[FETCH] Got {len(movies)} movies from page {page}")

        if not movies:
            return (0, 0, 0)

        # Fetch detailed data for all movies in page (parallel)
        movie_ids = [movie['id'] for movie in movies]
        detailed_movies = fetch_movies_parallel(movie_ids, max_workers=10)
        
        logger.info(f"[DETAIL] Fetched {len(detailed_movies)}/{len(movies)} movie details from page {page}")

        # Batch save all movies from this page
        if detailed_movies:
            saved, skipped, failed = batch_save_movies(detailed_movies)
            logger.info(f"[PAGE-DONE] Page {page}: Saved={saved}, Skipped={skipped}, Failed={failed}")
            return (saved, skipped, failed)

    except Exception as e:
        logger.error(f"Error processing page {page}: {e}")
    
    return (0, 0, 0)

def fetch_popular_movies_batch(start_page: int = 1, total_pages: int = 1, page_workers: int = 3):
    """Fetch popular movies in batches with parallel page processing"""
    total_saved = 0
    total_skipped = 0
    total_failed = 0

    logger.info(f"[START] Batch fetching {total_pages} page(s) from page {start_page} with {page_workers} parallel pages")
    start_time = time.time()

    pages = range(start_page, start_page + total_pages)
    
    # Process pages in parallel
    with ThreadPoolExecutor(max_workers=page_workers) as executor:
        future_to_page = {executor.submit(fetch_page_movies, page): page for page in pages}
        
        for future in as_completed(future_to_page):
            page = future_to_page[future]
            try:
                saved, skipped, failed = future.result()
                total_saved += saved
                total_skipped += skipped
                total_failed += failed
            except Exception as e:
                logger.error(f"Exception for page {page}: {e}")

    elapsed_time = time.time() - start_time

    logger.info(f"""
    ===================================
    [COMPLETED] Batch fetch completed in {elapsed_time:.2f}s!
    [OK] Saved: {total_saved}
    [SKIP] Skipped: {total_skipped}
    [ERROR] Failed: {total_failed}
    [SPEED] {total_saved / elapsed_time:.2f} movies/second
    ===================================
    """)

if __name__ == '__main__':
    # Batch configuration
    START_PAGE = 1          # Starting page
    TOTAL_PAGES = 10        # Number of pages to fetch
    PAGE_WORKERS = 3        # Number of pages to process in parallel (3-5 recommended)

    fetch_popular_movies_batch(start_page=START_PAGE, total_pages=TOTAL_PAGES, page_workers=PAGE_WORKERS)
