# AI Orchestrator Dependencies

This directory contains Docker Compose configuration for running the required services for the AI Orchestrator.

## Services

### PostgreSQL (Port 5432)
- **Purpose**: Primary database for user accounts, agent configurations, and plugin settings
- **Image**: postgres:16-alpine
- **Database**: orchestrator
- **Username**: orchestrator
- **Password**: orchestrator_dev_password (change in production!)
- **Data Volume**: `./postgres_data`

### ChromaDB (Port 8000)
- **Purpose**: Vector database for AI memories and embeddings
- **Image**: chromadb/chroma:0.6.3
- **API**: v1 (TODO: migrate to v2 API)
- **Data Volume**: `./chroma_data`

### Redis (Port 6379)
- **Purpose**: Short-term message caching and session storage
- **Image**: redis:latest
- **Data Volume**: `./redis_data`
- **Persistence**: Enabled (appendonly mode)

## Quick Start

### Start all services:
```bash
cd dependencies
docker-compose up -d
```

### Start specific service:
```bash
docker-compose up -d postgres
docker-compose up -d chromadb
docker-compose up -d redis
```

### Check service status:
```bash
docker-compose ps
```

### View logs:
```bash
docker-compose logs -f postgres
docker-compose logs -f chromadb
docker-compose logs -f redis
```

### Stop all services:
```bash
docker-compose down
```

### Stop and remove volumes (WARNING: deletes all data):
```bash
docker-compose down -v
```

## Database Connection Strings

### PostgreSQL
**Development**:
```
Host=localhost;Port=5432;Database=orchestrator;Username=orchestrator;Password=orchestrator_dev_password
```

**Connection via psql**:
```bash
psql -h localhost -p 5432 -U orchestrator -d orchestrator
```

### Redis
```
localhost:6379
```

### ChromaDB
```
http://localhost:8000/api/v1/
```

## Health Checks

### PostgreSQL
```bash
docker exec postgres pg_isready -U orchestrator
```

### Redis
```bash
docker exec redis redis-cli ping
```

### ChromaDB
```bash
curl http://localhost:8000/api/v1/heartbeat
```

## Initial Setup

After starting the services for the first time:

1. **Run EF Core migrations** (from project root):
```bash
cd Ai.Orchestrator.Services
dotnet ef database update
```

2. **Verify database created**:
```bash
psql -h localhost -p 5432 -U orchestrator -d orchestrator -c "\dt"
```

3. **Admin user** will be automatically seeded on first application run using:
   - Username: from `ADMIN_USERNAME` env variable (default: admin)
   - Password: from `ADMIN_PASSWORD` env variable (default: Admin@123456)

## Production Deployment

**IMPORTANT SECURITY NOTES** for production:

1. **Change all default passwords** in docker-compose.yml
2. **Use Docker secrets** instead of plain text passwords
3. **Enable SSL/TLS** for PostgreSQL connections
4. **Set up backups** for all data volumes
5. **Use strong JWT secret** (min 32 characters)
6. **Restrict network access** to database ports
7. **Enable authentication** on Redis
8. **Use managed database services** instead of Docker containers

## Data Volumes

All data is persisted in local directories:
- `./postgres_data` - PostgreSQL database files
- `./chroma_data` - ChromaDB vector store
- `./redis_data` - Redis persistence files

These directories are excluded from git (see `.gitignore`).

## Backup & Restore

### PostgreSQL Backup
```bash
docker exec postgres pg_dump -U orchestrator orchestrator > backup.sql
```

### PostgreSQL Restore
```bash
cat backup.sql | docker exec -i postgres psql -U orchestrator orchestrator
```

### Redis Backup
```bash
docker exec redis redis-cli SAVE
cp dependencies/redis_data/dump.rdb backup/dump.rdb
```

## Troubleshooting

### PostgreSQL won't start
- Check if port 5432 is already in use: `lsof -i :5432`
- Check logs: `docker-compose logs postgres`
- Remove old volume: `docker-compose down -v` (WARNING: deletes data)

### Redis won't start
- Check if port 6379 is already in use: `lsof -i :6379`
- Check logs: `docker-compose logs redis`

### ChromaDB won't start
- Check if port 8000 is already in use: `lsof -i :8000`
- Check logs: `docker-compose logs chromadb`

### Connection refused errors
- Ensure services are running: `docker-compose ps`
- Check health status: `docker-compose ps` (should show "healthy")
- Wait for health checks to pass (up to 30 seconds)
