# CineSocial - Social Movie Platform

A social movie platform built with .NET 9 and Clean Architecture, featuring movie browsing, ratings, reviews, and social interactions.

## 🚀 Features

- **Movie Database**: Browse and search movies with detailed information from TMDB
- **Social Features**: Follow users, rate movies, write reviews, and create custom movie lists
- **Authentication**: Secure JWT-based authentication
- **Real-time Monitoring**: Integrated observability stack with Elasticsearch, Kibana, Jaeger, and APM

## 🏗️ Architecture

- **Backend**: .NET 9, ASP.NET Core Web API
- **Architecture Pattern**: Clean Architecture + CQRS (MediatR)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 9
- **Logging**: Serilog with Elasticsearch
- **Tracing**: OpenTelemetry + Jaeger
- **Monitoring**: Elastic APM

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Python 3.8+](https://www.python.org/downloads/) (for data import script)
- [TMDB API Key](https://www.themoviedb.org/settings/api) (for movie data)

## 🛠️ Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd cinefeel
```

### 2. Configure Environment Variables

Copy the example environment file and fill in your values:

```bash
cd infrastructure
cp .env.example .env
# Edit .env with your values
```

**Important:** Update the following in `infrastructure/.env`:
- `TMDB_ACCESS_TOKEN`: Your TMDB API bearer token (get from https://www.themoviedb.org/settings/api)
- `JWT_SECRET`: A secure secret key (minimum 32 characters)
- `DATABASE_PASSWORD`: A secure database password

**Note:** This is the ONLY .env file you need (`infrastructure/.env`)! It's used by:
- Backend API (automatically found via DotNetEnv)
- Docker Compose (all 3 variants)
- Python import script (batch_fetch.py)

### 3. Start Infrastructure

Choose one of the following options:

#### Option A: PostgreSQL Only (Lightweight)

For minimal setup, just the database:

```bash
cd infrastructure
docker-compose up -d
```

#### Option B: PostgreSQL + Backend API (Recommended)

For full application without monitoring:

```bash
cd infrastructure
docker-compose -f docker-compose.simple.yml up -d --build
```

#### Option C: Full Stack with Monitoring (Advanced)

For complete observability stack:

```bash
cd infrastructure
docker-compose -f docker-compose.monitoring.yml up -d --build
```

### 4. Run Database Migrations

```bash
cd backend/src/CineSocial.Infrastructure
dotnet ef database update --startup-project ../CineSocial.Api
```

### 5. Import Movie Data (Optional)

Install Python dependencies:

```bash
pip install requests psycopg2-binary python-dotenv
```

Run the import script:

```bash
python batch_fetch.py
```

**Configuration** (in `batch_fetch.py`):
- `START_PAGE`: Starting page number (default: 1)
- `TOTAL_PAGES`: Number of pages to fetch (default: 10, ~200 movies)
- `PAGE_WORKERS`: Parallel workers (default: 3)

### 6. Run the Backend API

**Note:** If you used `docker-compose.simple.yml` or `docker-compose.monitoring.yml`, the backend is already running in Docker. Skip this step.

If you're running backend on your host machine:

#### Option A: From Command Line

```bash
cd backend/src/CineSocial.Api
dotnet run
```

#### Option B: From Visual Studio/Rider

Open `backend/CineSocial.sln` and press F5

## 🌐 Access Points

| Service | URL | Description |
|---------|-----|-------------|
| **API** | http://localhost:5047 | REST API |
| **Swagger** | http://localhost:5047/swagger | API Documentation |
| **Kibana** | http://localhost:5601 | Log Visualization |
| **Jaeger** | http://localhost:16686 | Distributed Tracing |
| **PostgreSQL** | localhost:5432 | Database |

## 👤 Default Users

Three test users are seeded automatically:

| Username | Email | Password | Role |
|----------|-------|----------|------|
| user | user@cinesocial.com | User123! | User |
| admin | admin@cinesocial.com | Admin123! | Admin |
| superuser | superuser@cinesocial.com | SuperUser123! | SuperUser |

## 📚 API Documentation

Once the API is running, visit http://localhost:5047/swagger for interactive API documentation.

### Main Endpoints

- **Authentication**: `/api/auth/register`, `/api/auth/login`
- **Movies**: `/api/movies`, `/api/movies/{id}`
- **Ratings**: `/api/movies/{movieId}/rates`
- **Comments**: `/api/movies/{movieId}/comments`
- **Movie Lists**: `/api/movie-lists`
- **Users**: `/api/users`, `/api/users/{userId}`
- **Search**: `/api/search/movies`, `/api/search/people`

## 🏗️ Project Structure

```
cinefeel/
├── backend/
│   ├── src/
│   │   ├── CineSocial.Api/              # REST API Layer
│   │   ├── CineSocial.Application/      # Business Logic (CQRS)
│   │   ├── CineSocial.Domain/           # Domain Entities
│   │   └── CineSocial.Infrastructure/   # Data Access & Services
│   └── tests/
├── infrastructure/
│   ├── .env                             # Environment variables (SINGLE FILE!)
│   ├── .env.example                     # Environment template
│   ├── docker-compose.yml               # PostgreSQL only
│   ├── docker-compose.simple.yml        # PostgreSQL + Backend API
│   └── docker-compose.monitoring.yml    # Full stack with monitoring
├── batch_fetch.py                       # Movie data import script
├── requirements.txt                     # Python dependencies
└── README.md                            # This file
```

## 🧪 Testing

### Run Unit Tests

```bash
cd backend
dotnet test
```

### Test API with Swagger

1. Navigate to http://localhost:5047/swagger
2. Click "Authorize" and enter a JWT token (obtain via `/api/auth/login`)
3. Test endpoints interactively

## 🐛 Troubleshooting

### Database Connection Issues

**Error:** "Connection refused" or "Could not connect to database"

**Solution:**
- Check if PostgreSQL is running: `docker ps`
- Verify `.env` file settings match docker-compose
- For backend on host: Use `DATABASE_HOST=localhost`
- For backend in Docker: Use `DATABASE_HOST=postgres`

### Migration Issues

**Error:** "A connection was successfully established... but then an error occurred"

**Solution:**
```bash
cd backend/src/CineSocial.Infrastructure
dotnet ef database drop --force --startup-project ../CineSocial.Api
dotnet ef database update --startup-project ../CineSocial.Api
```

### TMDB API Issues

**Error:** "TMDB_ACCESS_TOKEN not found"

**Solution:**
- Ensure `.env` file exists in project root
- Verify `TMDB_ACCESS_TOKEN` is set
- Get token from https://www.themoviedb.org/settings/api

### Port Already in Use

**Error:** "Address already in use"

**Solution:**
```bash
# Stop conflicting containers
docker-compose -f infrastructure/docker-compose.monitoring.yml down

# Or change ports in docker-compose files
```

## 📦 Dependencies

### Backend

- .NET 9.0
- Entity Framework Core 9.0
- MediatR
- FluentValidation
- Serilog
- OpenTelemetry
- Npgsql (PostgreSQL driver)

### Python Script

- requests
- psycopg2-binary
- python-dotenv

## 🔒 Security Notes

- **Never commit `.env` files** - They contain sensitive information
- Change default passwords before deploying to production
- Use strong JWT secrets (minimum 32 characters)
- Enable SSL/TLS for production database connections

## 📝 License

This project is licensed under the MIT License.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📧 Support

For issues and questions, please create an issue in the GitHub repository.

