# Existing Claims table compatibility

The original `Claims` table may contain older records with columns such as
`ItemId`, `ClaimerId`, and `SecretText`. It is **not** the same schema as the
new report-based workflow (`FoundReportId`, `ClaimantId`, and `Evidence`).

The new workflow maps `ClaimRequest` to **`ReportClaims`** instead of `Claims`.
`06_operations.sql` creates `ReportClaims` and has the new `HandoverRooms`
foreign key reference it. The original `Claims` table and its rows are not
changed or deleted. The admin/user claims views query `ReportClaims`.

**Important:** Existing legacy `Claims` entries are **not migrated** into
`ReportClaims`. They remain available for a separately planned migration
once `ItemId` -> `Reports.Id`, claimant identity, and workflow status mappings
have been verified. Do not rename or drop the old table.

For existing PostgreSQL databases, make a backup before applying the patch:

```powershell
# Within the Foundly repo root:
docker compose exec -T postgres pg_dump -U lostfound -d lostfound -Fc -f /tmp/foundly-before-claims-fix.dump
docker compose cp postgres:/tmp/foundly-before-claims-fix.dump .\foundly-before-claims-fix.dump
```

Apply the code patch, then rebuild API:

```powershell
docker compose up -d --build api
docker compose logs --tail=100 api
```

Verify both schemas and the unchanged legacy count:

```powershell
@'
SELECT table_name, column_name FROM information_schema.columns
WHERE table_schema = 'public' AND table_name IN ('Claims', 'ReportClaims')
ORDER BY table_name, ordinal_position;
SELECT COUNT(*) AS old_claim_rows FROM "Claims";
'@ | docker compose exec -T postgres psql -U lostfound -d lostfound
```

No database wipe or destructive migration is required.
