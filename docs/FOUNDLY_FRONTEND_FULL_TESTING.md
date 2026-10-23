# Foundly frontend — Full regression and system testing

Source baseline: `LostAndFound-CMC-main.zip` uploaded on 2026-10-23.

## Scope & unchanged files

Existing `backend/`, `database/`, `compose.yaml`, `README.md`, `frontend/src/components/ChatLogic.jsx`, `frontend/src/components/Sprint2ContactsView.jsx` and its CSS are **not changed**. `frontend/src/main.jsx` and stylesheet are **replaced** by the unified Pro interface, and `frontend/index.html` is updated. `frontend/package-lock.json` and Vite config are unchanged.

New UI has routes for matching, claims, operations, handovers and notifications, but without `VITE_API_PROFILE=full`, all five show a **Đang phát triển** page and never call nonexistent APIs. Do not enable `full` until implemented and tested.

## Tests performed offline (no running Docker/.NET)

- 8/8 *browser-based functional groups* with real React rendering in Chromium, simulated API: discovery/filters/search/return badge/detail; login/logout; register/verify; create/upload; personal reports/mark returned; contacts/chat/directory; five unavailable features; responsive 320/375/390/768/1024/1440.
- 1/1 baseline UI comparison: `RETURNED` previously displayed correctly and was repaired in Pro UI.
- Static source/contract tests for Sprint 2 and new frontend + chat transport mock tests all pass.
- Mockup Sprint 1 browser smoke test passed on six viewport sizes. **This is the older mockup**, separate from new React browser regression.
- Git patch application to uploaded main succeeds without conflicts; original backend, database and root README remain unchanged.

**Not yet verified**: production Vite build with Linux-native Rollup binaries, Docker/.NET/PostgreSQL live integration, real API round trips, security of deployed application, real photo storage, and multi-user full-system browser E2E. All require a functioning local environment or CI. No 100% bug-free or merge-success guarantee.

## CI (runs when PR is opened)

`.github/workflows/frontend-integration.yml` installs npm dependencies, performs `npm run build`, runs static/transport contracts, installs Playwright Chromium, runs older Sprint 1 mockup and runs `tests/frontend_full_e2e.py` against running Vite with intercepted API responses. This confirms frontend behavior **without database writes**, but CI mock API does not confirm the real backend or schema.

## Run manually on Windows PowerShell

From repo root:

```powershell
cd frontend
npm ci
npm run build
npm run dev
```

In a second terminal in repo root:

```powershell
python -m pip install playwright
python -m playwright install chromium
python tests/frontend_full_e2e.py
python tests/issue02_ui_smoke.py
python tests/issue13_schema_contract.py
python tests/issue14_ui_contract.py
python tests/issue15_frontend_contract.py
node --test tests/issue15_chat_api.test.cjs
```

Run the **real backend + database** in a development environment:

```powershell
docker compose up --build -d
docker compose ps
curl.exe http://127.0.0.1:8081/api/health
```

`tests/system_smoke_e2e.py` exercises the running ASP.NET Core API and PostgreSQL using two fresh development test users, posts, images, search, contacts, messages, access isolation, return status, and logout. **It permanently adds test records to the database**, so run ONLY on a test/development database and only with explicit opt-in:

```powershell
$env:FOUNDLY_ALLOW_TEST_WRITES = "1"
python tests/system_smoke_e2e.py
Remove-Item Env:FOUNDLY_ALLOW_TEST_WRITES
```

Ensure `ASPNETCORE_ENVIRONMENT=Development`; the script needs `devVerificationCode` from register and cannot run in production. If real Docker/API system testing fails, **do not merge**. Address failing endpoint and re-run.

## Manual browser acceptance before merge

Use two accounts to verify both sides of real chat: log in, create LOST and FOUND posts, search, filter, view details, upload valid image, access My Posts, mark one report RETURNED, open new contact from the non-owner account, exchange messages, check directory, log out and log in as other user; test privacy by confirming the second account cannot access private messages or mark another user's post returned. Check all 5 "Đang phát triển" links without HTTP 404, and validate layout at 320/375/390/768/1024/1440 width. Do not claim matching, claims, or administration is functional without their APIs.

**Merge requirement**: CI green **and** development Docker/API tests green **and** the manual multi-account acceptance completed, plus approval from the repo team.
