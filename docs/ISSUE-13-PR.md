# feat(sprint2): Anonymous Contact and Directory API (#13)

## Summary
- `Contacts`, `Messages`, `Directories` models and idempotent PostgreSQL schema update for existing Sprint 1 DB.
- Authenticated participant-only contact/message REST endpoints and public institutional directory.
- Content limits, rate limiting, read-only on closed reports, explicit safe DTOs excluding personal email/phone and private identifiers.
- Integration smoke script and Postman collection.

## Validation
- [ ] `docker compose up --build -d` succeeds with database fresh **and** existing Sprint 1 volume.
- [ ] `python tests/issue13_contacts_api.py` reports PASS.
- [ ] Manually verify POST/GET endpoints and privacy in Postman.
- [ ] Existing Sprint 1 posting/auth features still work.

## Scope
Only Issue #13. UI belongs to PR #14; API rendering/integration belongs to #15. **Do not merge before verification.**
Closes #13
