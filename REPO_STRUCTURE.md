# CineFeel Repository Structure and Architecture Guide

## 📖 Table of Contents
1. [Repository Overview](#repository-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture Pattern](#architecture-pattern)
4. [Project Structure](#project-structure)
5. [Backend Architecture](#backend-architecture)
6. [Infrastructure Setup](#infrastructure-setup)
7. [Key Features](#key-features)
8. [Development Workflow](#development-workflow)
9. [API Endpoints](#api-endpoints)
10. [Testing Strategy](#testing-strategy)

---

## 🎬 Repository Overview

**CineFeel (CineSocial)** is a social movie platform built with modern .NET technologies, featuring:
- Movie browsing and detailed information from TMDB (The Movie Database)
- Social interactions: ratings, reviews, comments, and user following
- Custom movie lists and collections
- JWT-based authentication and authorization
- Real-time monitoring and observability with OpenTelemetry

**Repository URL**: CineSocial-AI/cinefeel

---

## 🛠️ Technology Stack

### Backend Technologies
- **.NET 9.0**: Latest .NET framework
- **ASP.NET Core Web API**: REST API implementation
- **Entity Framework Core 9**: ORM for database operations
- **PostgreSQL 16**: Primary database
- **Redis 7**: L2 caching with FusionCache (hybrid L1 memory + L2 Redis)

### Architecture & Patterns
- **Clean Architecture**: Separation of concerns across layers
- **CQRS (Command Query Responsibility Segregation)**: Using MediatR
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Built-in ASP.NET Core DI

### Key Libraries & Frameworks
- **MediatR**: CQRS implementation and request/response pattern
- **FluentValidation**: Input validation
- **Serilog**: Structured logging
- **OpenTelemetry**: Distributed tracing and metrics
- **HotChocolate**: GraphQL implementation (optional)
- **DotNetEnv**: Environment variable management

### Observability Stack (Optional)
- **Elasticsearch**: Log aggregation and search
- **Kibana**: Log visualization and dashboards
- **Jaeger**: Distributed tracing UI
- **APM**: Application Performance Monitoring
- **Prometheus & Grafana**: Metrics and monitoring

### Frontend
- Basic HTML files for verification (`index.html`, `verify.html`)
- Frontend implementation is minimal/planned

### DevOps & Infrastructure
- **Docker & Docker Compose**: Containerization
- **Docker Compose variants**:
  - `docker-compose.db.yml`: PostgreSQL only (lightweight)
  - `docker-compose.redis.yml`: Redis cache
  - `docker-compose.telemetry.yml`: Full observability stack

### Data Import
- **Python 3.8+**: Data import scripts
- **TMDB API**: Movie data source
- Dependencies: `requests`, `psycopg2-binary`, `python-dotenv`

---

## 🏗️ Architecture Pattern

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                  (CineSocial.Api)                       │
│  Controllers, Middleware, Health Checks, Telemetry      │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                   Application Layer                      │
│                (CineSocial.Application)                 │
│  Features (CQRS), Commands, Queries, DTOs, Validation   │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                    Domain Layer                          │
│                  (CineSocial.Domain)                    │
│    Entities, Value Objects, Domain Logic, Enums         │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                 Infrastructure Layer                     │
│              (CineSocial.Infrastructure)                │
│  Data Access, Repositories, External Services, Cache    │
└─────────────────────────────────────────────────────────┘
```

### CQRS Pattern with MediatR

- **Commands**: Modify state (Create, Update, Delete)
- **Queries**: Read data (Get, List, Search)
- **Handlers**: Process commands and queries
- **Behaviors**: Cross-cutting concerns (validation, logging, caching)

---

## 📁 Project Structure

```
cinefeel/
├── backend/                          # .NET Backend Application
│   ├── src/
│   │   ├── CineSocial.Api/          # 🌐 REST API Layer
│   │   │   ├── Controllers/         # API endpoints
│   │   │   ├── Middleware/          # Request pipeline middleware
│   │   │   ├── HealthChecks/        # Health monitoring
│   │   │   ├── Telemetry/           # OpenTelemetry configuration
│   │   │   ├── Program.cs           # Application entry point
│   │   │   └── Dockerfile           # Container configuration
│   │   │
│   │   ├── CineSocial.Application/  # 💼 Business Logic Layer
│   │   │   ├── Features/            # CQRS features by domain
│   │   │   │   ├── Auth/            # Authentication & Authorization
│   │   │   │   ├── Movies/          # Movie operations
│   │   │   │   ├── Comments/        # Comment operations
│   │   │   │   ├── Rates/           # Rating operations
│   │   │   │   ├── MovieLists/      # Movie list operations
│   │   │   │   ├── Users/           # User management
│   │   │   │   ├── People/          # Cast & crew operations
│   │   │   │   ├── Search/          # Search functionality
│   │   │   │   └── Images/          # Image handling
│   │   │   ├── Common/              # Shared components
│   │   │   │   ├── Behaviors/       # MediatR pipeline behaviors
│   │   │   │   ├── Exceptions/      # Custom exceptions
│   │   │   │   ├── Interfaces/      # Abstractions
│   │   │   │   ├── Models/          # DTOs and response models
│   │   │   │   ├── Results/         # Result pattern
│   │   │   │   └── Security/        # Security utilities
│   │   │   └── Services/            # Application services
│   │   │
│   │   ├── CineSocial.Domain/       # 🎯 Domain Layer
│   │   │   ├── Entities/            # Domain entities
│   │   │   │   ├── Movie/           # Movie-related entities
│   │   │   │   │   ├── MovieEntity.cs
│   │   │   │   │   ├── Genre.cs
│   │   │   │   │   ├── Person.cs
│   │   │   │   │   ├── MovieCast.cs
│   │   │   │   │   ├── MovieCrew.cs
│   │   │   │   │   └── ... (other movie entities)
│   │   │   │   ├── User/            # User entities
│   │   │   │   ├── Social/          # Social entities (comments, rates, lists)
│   │   │   │   └── Jobs/            # Background job entities
│   │   │   ├── Common/              # Base entities, interfaces
│   │   │   └── Enums/               # Domain enumerations
│   │   │
│   │   └── CineSocial.Infrastructure/ # 🔧 Infrastructure Layer
│   │       ├── Data/                # Database context and config
│   │       │   ├── Configurations/  # EF Core entity configurations
│   │       │   └── Seed/            # Database seeding
│   │       ├── Migrations/          # EF Core migrations
│   │       ├── Repositories/        # Repository implementations
│   │       ├── Caching/             # FusionCache (L1 + L2 Redis)
│   │       ├── Services/            # External service integrations
│   │       ├── Security/            # JWT, password hashing
│   │       ├── CloudStorage/        # File storage
│   │       ├── Email/               # Email services
│   │       │   └── Templates/       # Email templates
│   │       └── Jobs/                # Background jobs
│   │           ├── Base/            # Job abstractions
│   │           ├── Configuration/   # Job setup
│   │           ├── Email/           # Email jobs
│   │           └── Services/        # Job services
│   │
│   ├── tests/                       # Unit and Integration Tests
│   │   ├── CineSocial.Domain.UnitTests/
│   │   └── CineSocial.Application.UnitTests/
│   │       └── Features/            # Feature-based test organization
│   │
│   ├── CineSocial.sln              # Visual Studio solution file
│   ├── Dockerfile                   # Backend container image
│   └── .dockerignore               # Docker ignore patterns
│
├── infrastructure/                  # 🐳 Docker & Configuration
│   ├── .env.example                # Environment template
│   ├── docker-compose.db.yml       # PostgreSQL container
│   ├── docker-compose.redis.yml    # Redis cache container
│   ├── docker-compose.telemetry.yml # Monitoring stack
│   ├── prometheus/                  # Prometheus configuration
│   ├── grafana/                     # Grafana dashboards
│   ├── loki/                        # Loki log aggregation
│   └── promtail/                    # Promtail log collection
│
├── scripts/                         # 🐍 Python Utility Scripts
│   ├── insert_data_to_db.py        # Import data from CSV to database
│   ├── fetch_other_values.py       # Fetch additional TMDB data
│   └── run_log_other_values.json   # Script execution logs
│
├── frontend/                        # 🌐 Frontend Files (minimal)
│   ├── index.html                  # Main page
│   └── verify.html                 # Verification page
│
├── .github/                         # GitHub configuration
│   └── agents/                      # CI/CD agents (not to be read)
│
├── README.md                        # Main documentation
├── SETUP.md                         # Quick setup guide
├── howtorun.txt                     # Turkish setup instructions
├── requirements.txt                 # Python dependencies
├── .gitignore                       # Git ignore patterns
└── REPO_STRUCTURE.md               # This file
```

---

## 🏛️ Backend Architecture

### Layer Responsibilities

#### 1. **API Layer (CineSocial.Api)**
- **Purpose**: HTTP request/response handling
- **Components**:
  - `Controllers/`: REST API endpoints
  - `Middleware/`: Exception handling, logging
  - `HealthChecks/`: Application health monitoring
  - `Telemetry/`: OpenTelemetry setup
  - `Program.cs`: Dependency injection, middleware pipeline

#### 2. **Application Layer (CineSocial.Application)**
- **Purpose**: Business logic and use cases
- **Components**:
  - `Features/`: Vertical slice architecture by feature
    - Each feature contains Commands and Queries
    - Handlers process the business logic
  - `Common/Behaviors/`: Validation, logging, caching pipelines
  - `Services/`: Application services

**Example Feature Structure**:
```
Features/Movies/
├── Commands/
│   ├── FavoriteMovie/
│   │   ├── FavoriteMovieCommand.cs
│   │   ├── FavoriteMovieCommandHandler.cs
│   │   └── FavoriteMovieCommandValidator.cs
│   └── UnfavoriteMovie/
└── Queries/
    ├── GetMovieDetail/
    │   ├── GetMovieDetailQuery.cs
    │   ├── GetMovieDetailQueryHandler.cs
    │   └── MovieDetailDto.cs
    └── GetMovies/
```

#### 3. **Domain Layer (CineSocial.Domain)**
- **Purpose**: Core business entities and logic
- **Components**:
  - `Entities/`: Domain models (Movie, User, etc.)
  - `Common/`: Base entity classes
  - `Enums/`: Domain enumerations

**Key Entities**:
- `MovieEntity`: Core movie information
- `Person`: Cast and crew members
- `MovieCast`, `MovieCrew`: Movie-person relationships
- `Genre`, `Country`, `Language`: Reference data
- `Comment`, `Rate`: Social interactions
- `MovieList`: User-created collections
- `User`: Application users

#### 4. **Infrastructure Layer (CineSocial.Infrastructure)**
- **Purpose**: External concerns and data access
- **Components**:
  - `Data/`: EF Core DbContext and configurations
  - `Repositories/`: Data access implementations
  - `Caching/`: FusionCache (L1 memory + L2 Redis)
  - `Services/`: External API integrations (TMDB)
  - `Security/`: JWT token generation, password hashing
  - `Jobs/`: Background job processing

---

## 🐳 Infrastructure Setup

### Docker Compose Variants

1. **PostgreSQL Only** (`docker-compose.db.yml`)
   - Minimal setup for development
   - PostgreSQL 16 on port 5432
   - Persistent volume for data

2. **Redis Cache** (`docker-compose.redis.yml`)
   - Redis 7 on port 6379
   - Used as L2 cache with FusionCache
   - 256MB memory limit with LRU eviction

3. **Telemetry Stack** (`docker-compose.telemetry.yml`)
   - Elasticsearch, Kibana, Jaeger, APM
   - Prometheus & Grafana
   - Loki & Promtail for log aggregation

### Environment Configuration

**Single .env File**: `infrastructure/.env`

All configurations use this single file:
- Docker Compose (automatically finds it)
- Backend API (via DotNetEnv)
- Python scripts (via python-dotenv)

**Key Environment Variables**:
```bash
# TMDB API
TMDB_ACCESS_TOKEN=your_token

# Database
DATABASE_HOST=localhost  # or 'postgres' in Docker
DATABASE_PORT=5432
DATABASE_NAME=CINE
DATABASE_USER=cinesocial
DATABASE_PASSWORD=your_password

# JWT
JWT_SECRET=32_character_minimum_secret
JWT_ISSUER=CineSocial
JWT_AUDIENCE=CineSocialUsers
JWT_EXPIRE_MINUTES=60

# Redis
REDIS_CONNECTION_STRING=localhost:6379
```

### Caching Strategy

**FusionCache** - Hybrid multi-level cache:
- **L1 (Memory)**: In-process cache for fastest access
- **L2 (Redis)**: Distributed cache for consistency

**Cache Durations**:
- Reference Data (Genre, Country, Language): 24 hours
- Movies & People: 1 hour
- User Data: 15 minutes
- Comments & Rates: 5 minutes
- Search Results: 15 minutes

**Auto-invalidation**: Mutation operations automatically clear related cache

---

## ✨ Key Features

### 1. **Authentication & Authorization**
- JWT-based authentication
- Role-based access control (User, Admin, SuperUser)
- Three default seeded users

### 2. **Movie Management**
- Browse movies with pagination
- Filter by genre, country, language
- Movie details with cast, crew, images, videos
- TMDB integration for real-time data

### 3. **Social Features**
- Rate movies (0-10 scale)
- Write and read comments
- Create custom movie lists
- Follow/unfollow users
- Favorite movies

### 4. **Search**
- Search movies by title, genre, etc.
- Search people (cast/crew)

### 5. **Image Handling**
- Movie posters and backdrops
- Person profile images
- Cloud storage integration ready

### 6. **Observability**
- Structured logging with Serilog
- Distributed tracing with OpenTelemetry
- Health checks for dependencies
- Metrics and APM

---

## 🔄 Development Workflow

### Initial Setup

1. **Clone Repository**
   ```bash
   git clone <repository-url>
   cd cinefeel
   ```

2. **Configure Environment**
   ```bash
   cd infrastructure
   cp .env.example .env
   # Edit .env with your values (TMDB token, passwords, etc.)
   ```

3. **Start Infrastructure**
   ```bash
   # Option A: PostgreSQL only
   docker-compose -f docker-compose.db.yml up -d
   
   # Option B: With Redis cache
   docker-compose -f docker-compose.db.yml up -d
   docker-compose -f docker-compose.redis.yml up -d
   ```

4. **Run Migrations**
   ```bash
   cd ../backend/src/CineSocial.Infrastructure
   dotnet ef database update --startup-project ../CineSocial.Api
   ```

5. **Run Backend**
   ```bash
   cd ../CineSocial.Api
   dotnet run
   ```

6. **Import Movie Data (Optional)**
   ```bash
   cd ../../../..
   pip install -r requirements.txt
   python scripts/insert_data_to_db.py
   python scripts/fetch_other_values.py
   ```

### Building & Testing

```bash
# Build solution
cd backend
dotnet build

# Run tests
dotnet test

# Run specific test project
dotnet test tests/CineSocial.Application.UnitTests/

# Create migration
cd src/CineSocial.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../CineSocial.Api

# Update database
dotnet ef database update --startup-project ../CineSocial.Api
```

---

## 🌐 API Endpoints

### Base URL
- Development: `http://localhost:5047`
- Swagger UI: `http://localhost:5047/swagger`

### Main Endpoints

#### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

#### Movies
- `GET /api/movies` - List movies (paginated)
- `GET /api/movies/{id}` - Get movie details
- `GET /api/movies/filtered` - Filter movies by criteria
- `POST /api/movies/{movieId}/favorite` - Favorite a movie
- `DELETE /api/movies/{movieId}/favorite` - Unfavorite a movie

#### Ratings
- `POST /api/movies/{movieId}/rates` - Rate a movie
- `GET /api/movies/{movieId}/rates` - Get movie ratings
- `PUT /api/movies/{movieId}/rates/{rateId}` - Update rating
- `DELETE /api/movies/{movieId}/rates/{rateId}` - Delete rating

#### Comments
- `POST /api/movies/{movieId}/comments` - Add comment
- `GET /api/movies/{movieId}/comments` - Get comments
- `PUT /api/movies/{movieId}/comments/{commentId}` - Update comment
- `DELETE /api/movies/{movieId}/comments/{commentId}` - Delete comment

#### Movie Lists
- `GET /api/movie-lists` - Get user's lists
- `POST /api/movie-lists` - Create list
- `GET /api/movie-lists/{id}` - Get list details
- `PUT /api/movie-lists/{id}` - Update list
- `DELETE /api/movie-lists/{id}` - Delete list
- `POST /api/movie-lists/{id}/movies` - Add movie to list
- `DELETE /api/movie-lists/{id}/movies/{movieId}` - Remove movie from list

#### Users
- `GET /api/users` - List users
- `GET /api/users/{userId}` - Get user profile
- `POST /api/users/{userId}/follow` - Follow user
- `DELETE /api/users/{userId}/follow` - Unfollow user

#### Search
- `GET /api/search/movies` - Search movies
- `GET /api/search/people` - Search people

#### People
- `GET /api/people/{id}` - Get person details

#### Images
- `GET /api/images` - Get image URLs

---

## 🧪 Testing Strategy

### Test Projects

1. **CineSocial.Domain.UnitTests**
   - Domain entity tests
   - Business logic validation

2. **CineSocial.Application.UnitTests**
   - Command handler tests
   - Query handler tests
   - Validation tests
   - Feature-organized test structure

### Test Organization

Tests follow the same feature structure as the application:
```
tests/CineSocial.Application.UnitTests/
└── Features/
    ├── Movies/
    │   ├── Commands/
    │   │   └── FavoriteMovie/
    │   │       └── FavoriteMovieCommandHandlerTests.cs
    │   └── Queries/
    │       └── GetMovieDetail/
    │           └── GetMovieDetailQueryHandlerTests.cs
    └── MovieLists/
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/CineSocial.Application.UnitTests/

# With coverage (if configured)
dotnet test /p:CollectCoverage=true
```

---

## 🔐 Security Features

1. **JWT Authentication**
   - Bearer token-based
   - Configurable expiration
   - Role-based authorization

2. **Password Security**
   - Hashed with strong algorithms
   - Minimum strength requirements

3. **Input Validation**
   - FluentValidation for all commands
   - Request validation pipeline

4. **Secrets Management**
   - Environment variables via .env
   - No hardcoded credentials
   - .env files in .gitignore

---

## 📊 Monitoring & Observability

### Health Checks
- `GET /health` - Basic health status
- `GET /health-ui` - Health check UI

### Logging
- Structured logging with Serilog
- Log levels: Debug, Information, Warning, Error
- Console and file outputs
- Optional Elasticsearch integration

### Tracing
- OpenTelemetry distributed tracing
- Jaeger UI for trace visualization
- Automatic span creation for HTTP requests

### Metrics
- Prometheus metrics endpoint
- Grafana dashboards
- Application performance monitoring

---

## 🚀 Getting Started Quick Reference

```bash
# 1. Setup environment
cd infrastructure && cp .env.example .env
# Edit .env with your values

# 2. Start database
docker-compose -f docker-compose.db.yml up -d

# 3. Run migrations
cd ../backend/src/CineSocial.Infrastructure
dotnet ef database update --startup-project ../CineSocial.Api

# 4. Run backend
cd ../CineSocial.Api
dotnet run

# 5. Access API
# Swagger: http://localhost:5047/swagger
# Health: http://localhost:5047/health
```

---

## 📝 Additional Resources

- **README.md**: Main project documentation
- **SETUP.md**: Quick setup guide
- **howtorun.txt**: Turkish setup instructions (legacy)
- **Swagger UI**: Interactive API documentation at `/swagger`

---

## 🤝 Contributing

1. Follow Clean Architecture principles
2. Use CQRS pattern for new features
3. Write unit tests for handlers
4. Update documentation for new features
5. Follow existing code conventions

---

**Last Updated**: November 2024
**Version**: .NET 9.0, PostgreSQL 16, Redis 7
