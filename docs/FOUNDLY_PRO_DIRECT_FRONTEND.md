# Foundly Pro UI integrated into `frontend/`

Source base: `LostAndFound-CMC-main.zip` provided on 2026-10-23.

## What changed

- **Replaced only the existing React page and stylesheet** (`frontend/src/main.jsx`, `frontend/src/style.css`) with the Foundly Pro user-facing design.
- Updated `frontend/index.html` for page fonts and product title.
- Kept existing API backend, PostgreSQL, Docker Compose, and README unchanged.
- **Retained Issue #13/14/15 chat workflow** through existing `ChatLogic.jsx`, `Sprint2ContactsView.jsx`, and `sprint2Contacts.css`.
- Shows matching, claims, notifications, custody/support, and handover tabs as **Sắp có**. Clicking these opens an explanatory placeholder screen; no missing API endpoint is called.
- `VITE_API_PROFILE` is **not** set in `compose.yaml`; the safe default is Sprint mode. Do NOT enable `VITE_API_PROFILE=full` without implementing and testing all Full Operations APIs.
- Separate admin frontend is **not** activated or merged into the main user's app. Admin management APIs do not yet exist in this repo.

## Existing live functionality

Login/register/verify/logout; search and filter items; view details; create a report with image uploads; view own reports; mark returned; open a contact for another user's report; send and read real messages; use internal directory.

## Test steps

```powershell
python tests/integrated_frontend_contract.py
python tests/issue15_frontend_contract.py
python tests/issue14_ui_contract.py
cd frontend
npm ci
npm run build
cd ..
node tests/integrated_frontend_syntax.mjs
```

Run whole system (with Docker and real APIs) using:

```powershell
docker compose up --build -d
```

Then open **http://localhost:5175**. Verify login, create report, search, clicking a card, "Liên hệ người đăng" (with a DIFFERENT account), messages, internal directory, mark returned, and 5 upcoming buttons. The Docker/browser E2E requires a running environment and is not asserted by the source-only tests.

## Merge and follow-up

Apply patch onto fresh main using `git apply --check`, then `git apply`, test and push branch for PR. GitHub Actions will run npm installation and a real Vite build. Preserve original main's backend and deployment configuration. Avoid applying the older patch that created `frontend-pro/` and `admin-pro/`: this one changes the existing `frontend/` directly.

## Full regression update (2026-10-23)

See `docs/FOUNDLY_FRONTEND_FULL_TESTING.md`. The previous 12 static compatibility checks alone were not sufficient. A Chromium test found that `RETURNED` reports had lost their visible "ĐÃ TRẢ" badge; this has been fixed in both list and detail. Full mock-API React UI regression now covers all existing core user flows and six viewport sizes. Live backend/DB integration is pending and should be verified with `tests/system_smoke_e2e.py` on a development environment before merging.
