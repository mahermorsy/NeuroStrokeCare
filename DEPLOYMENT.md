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

## Optional environment variables (password-reset emails)

Leave `SMTP_HOST` blank to keep email sending disabled — the API just logs a
warning and "forgot password" requests succeed without actually sending
anything. Set all of these to enable real reset emails:

- `SMTP_HOST`
- `SMTP_PORT` (default 587)
- `SMTP_USERNAME`
- `SMTP_PASSWORD`
- `SMTP_ENABLE_SSL` (default true)
- `SMTP_FROM_ADDRESS`
- `SMTP_FROM_NAME` (default "NeuroStrokeCare")
- `FRONTEND_RESET_URL` — the public URL of the deployed frontend's
  reset-password page (e.g. `https://your-domain.example/reset-password`),
  not `localhost`

## Notes

- The committed `appsettings.json` intentionally does not contain secrets.
- The frontend Nginx config proxies `/api/*` and `/uploads/*` to the API container using the Docker service name `api`.
- Staff ID card photos are stored on the API container's own disk under `App_Data/uploads`, backed by the `neurostroke_uploads` Docker volume so they survive redeploys. No external storage (S3, etc.) is used.
- EF Core migrations exist under `NeuroStrokeCare.infrastructure/Migrations`, but the API currently does not automatically call `Database.Migrate()` at startup.
