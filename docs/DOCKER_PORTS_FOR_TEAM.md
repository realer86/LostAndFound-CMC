# Docker ports for the Foundly team

Use `docker compose up -d --build` at the repository root. Default host endpoints:

| Service | Host default | Container internal | Check |
|---|---:|---:|---|
| Admin | 5174 | 5174 | http://localhost:5174/ |
| Web | 5175 | 5175 | http://localhost:5175/ |
| API | 8081 | 8080 | http://localhost:8081/api/health |
| PostgreSQL | 5435 | 5432 | TCP/psql (not HTTP) |

If a port is already in use on **your computer**, create a local, gitignored `.env`:

```dotenv
FOUNDLY_ADMIN_PORT=5174
FOUNDLY_WEB_PORT=5175
FOUNDLY_API_PORT=8081
FOUNDLY_DB_PORT=5435
```

Change **only** whichever host port conflicts, e.g. `FOUNDLY_WEB_PORT=5185` or `FOUNDLY_API_PORT=8181`. The project uses the defaults when variables are absent. Never change the container ports on the right-hand side of `ports`, or the internal `API_PROXY_TARGET: http://api:8080` / `Host=postgres;Port=5432`. Compose passes the selected frontend/admin/api host ports to the API's localhost Origin allowlist. The Admin links to the configured Web host port.

After editing `.env`, run:

```powershell
docker compose up -d --build --force-recreate
docker compose ps
powershell -ExecutionPolicy Bypass -File .\scripts\check-services.ps1
```

This script checks Admin, Web, API by HTTP and PostgreSQL by TCP. It does not perform login, database migrations, or browser E2E; run the normal test suite and GitHub Actions before merging. Startup/build success alone is insufficient.

If Docker reports `port is already allocated`, first inspect `docker ps -a` and `netstat -ano | findstr :PORT`, then stop the **correct** old service if it is safe to do so. An app cannot automatically reclaim another process's bound port. Changes to `.env` are per-machine; do not commit your `.env` (it may contain secrets). Do not run `docker compose down -v` if you need to retain database data.

Note: `name: foundly-project` is shared across cloned directories on the same computer. For concurrent clones on **one computer**, set `COMPOSE_PROJECT_NAME` separately and ensure host ports don't overlap. Changing the project name also creates separate named Docker volumes by default; old data is not automatically migrated.
