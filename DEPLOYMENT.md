# NeuroStrokeCare Deployment Notes

This repository contains:

- `NeuroStrokeCare.api`: ASP.NET Core API targeting .NET 10, listening on port 8080 in Docker.
- `NeuroStrokeCare.client`: React/Vite frontend served by Nginx on port 80 in Docker.
- `docker-compose.prod.yml`: production-oriented Docker Compose with SQL Server, API, and client.

## Before running on a server

1. Copy `.env.example` to `.env` and replace every value.
2. Do not commit `.env`.
3. Use `docker-compose.prod.yml` for hosting:

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

## Required environment variables

- `MSSQL_SA_PASSWORD`
- `JWT_KEY`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_DURATION_MINUTES`
- `DB_NAME`

## Notes

- The committed `appsettings.json` intentionally does not contain secrets.
- The frontend Nginx config proxies `/api/*` to the API container using the Docker service name `api`.
- EF Core migrations exist under `NeuroStrokeCare.infrastructure/Migrations`, but the API currently does not automatically call `Database.Migrate()` at startup.
