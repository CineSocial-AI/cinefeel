# Quick Setup Guide

## 🎯 Single .env Configuration

This project uses **ONE single `.env` file** located in `infrastructure/` folder for all configuration!

### Setup Steps

1. **Copy the example file:**
   ```bash
   cd infrastructure
   cp .env.example .env
   ```

2. **Edit `infrastructure/.env` and fill in your values:**
   - `TMDB_ACCESS_TOKEN`: Get from https://www.themoviedb.org/settings/api
   - `JWT_SECRET`: Use a secure 32+ character string
   - `DATABASE_PASSWORD`: Choose a secure password

3. **That's it!** This single `infrastructure/.env` file is used by:
   - ✅ Docker Compose (automatically found - same folder)
   - ✅ Backend API (automatically found via DotNetEnv)
   - ✅ Python import script (automatically found via path)

## 🚀 Quick Start

### Option 1: Simple (Recommended)
```bash
# 1. Setup environment
cd infrastructure
cp .env.example .env
# Edit .env with your values

# 2. Start everything in Docker
docker-compose -f docker-compose.simple.yml up -d --build

# 3. Run migrations
cd ../backend/src/CineSocial.Infrastructure
dotnet ef database update --startup-project ../CineSocial.Api

# 4. Import movies
cd ../../../..
pip install -r requirements.txt
python batch_fetch.py

# Done! API: http://localhost:5047/swagger
```

### Option 2: Development (Backend on Host)
```bash
# 1. Setup environment
cd infrastructure
cp .env.example .env
# Edit .env with your values

# 2. Start only PostgreSQL
docker-compose up -d

# 3. Run migrations
cd ../backend/src/CineSocial.Infrastructure
dotnet ef database update --startup-project ../CineSocial.Api

# 4. Run backend
cd ../CineSocial.Api
dotnet run

# 5. Import movies (in another terminal)
cd ../../..
pip install -r requirements.txt
python batch_fetch.py

# Done! API: http://localhost:5047/swagger
```

### Option 3: Full Stack with Monitoring
```bash
# 1. Setup environment
cd infrastructure
cp .env.example .env
# Edit .env with your values

# 2. Uncomment observability settings in .env
# ELASTICSEARCH_URL=http://localhost:9200
# JAEGER_OTLP_ENDPOINT=http://localhost:4317
# etc.

# 3. Start full stack
docker-compose -f docker-compose.monitoring.yml up -d --build

# 4. Run migrations
cd ../backend/src/CineSocial.Infrastructure
dotnet ef database update --startup-project ../CineSocial.Api

# 5. Import movies
cd ../../../..
pip install -r requirements.txt
python batch_fetch.py

# Done!
# - API: http://localhost:5047/swagger
# - Kibana: http://localhost:5601
# - Jaeger: http://localhost:16686
```

## 📝 Important Notes

### Single .env Location

The `.env` file is located at: `infrastructure/.env`

**Why infrastructure folder?**
- ✅ Docker Compose runs from this folder (no --env-file needed)
- ✅ Backend automatically finds it (multiple path checks)
- ✅ Python script automatically finds it (relative path)
- ✅ Simpler than root `.env`

### DATABASE_HOST Setting

The `.env` file has `DATABASE_HOST=localhost` by default.

- **If backend runs on host machine:** Keep `DATABASE_HOST=localhost` ✅
- **If backend runs in Docker:** It's automatically overridden to `postgres` in docker-compose files ✅

You don't need to change anything - it works automatically!

### Only ONE .env File!

Previously required (❌ OLD):
- ~~`.env`~~ (root)
- ~~`backend/.env`~~
- ~~`infrastructure/.env`~~

Now only need (✅ NEW):
- `infrastructure/.env` (SINGLE FILE!)

## 🔒 Security

- `infrastructure/.env` is in `.gitignore` - will NOT be committed
- `infrastructure/.env.example` is tracked - safe template for others
- Always keep sensitive values in `.env` only

## 🆘 Troubleshooting

### "TMDB_ACCESS_TOKEN not found"
- Make sure `infrastructure/.env` exists
- Check that `TMDB_ACCESS_TOKEN` is set (no quotes needed)

### "Connection refused" to database
- Check Docker: `docker ps` (should see `cinesocial-postgres`)
- Verify `infrastructure/.env` has correct database credentials

### Backend can't find .env
- Make sure `.env` is in `infrastructure/` folder
- Backend tries multiple paths automatically

### Docker Compose warnings about variables
- Make sure you're in `infrastructure/` folder when running docker-compose
- Verify `infrastructure/.env` exists

## 🎬 Docker Commands (All from infrastructure/ folder)

```bash
cd infrastructure

# Start PostgreSQL only
docker-compose up -d

# Start PostgreSQL + Backend
docker-compose -f docker-compose.simple.yml up -d --build

# Start full stack
docker-compose -f docker-compose.monitoring.yml up -d --build

# Stop
docker-compose down
# or
docker-compose -f docker-compose.simple.yml down

# View logs
docker-compose logs -f
docker-compose logs -f cinesocial-api

# Restart
docker-compose restart
```

For more help, see full [README.md](README.md)
