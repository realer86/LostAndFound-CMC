"""Static regression guard: legacy Claims must remain untouched.
Does not replace a PostgreSQL runtime test.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sql = (root / "backend/DatabaseScripts/06_operations.sql").read_text(encoding="utf-8")
db = (root / "backend/Data/LostFoundDb.cs").read_text(encoding="utf-8")

assert 'CREATE TABLE IF NOT EXISTS "ReportClaims"' in sql
assert 'ON "ReportClaims"("FoundReportId","ClaimantId")' in sql
assert 'REFERENCES "ReportClaims"("Id")' in sql
assert 'b.Entity<ClaimRequest>().ToTable("ReportClaims")' in db
assert 'CREATE TABLE IF NOT EXISTS "Claims"' not in sql
assert 'ON "Claims"(' not in sql
assert 'REFERENCES "Claims"(' not in sql
assert 'DROP TABLE "Claims"' not in sql
assert 'ALTER TABLE "Claims"' not in sql
print("PASS: new report claims schema isolated from legacy Claims")
