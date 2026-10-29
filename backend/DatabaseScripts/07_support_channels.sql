-- Official support inbox. Additive only: preserve all legacy Contacts/Messages and Claims.
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DepartmentCode" varchar(20) NULL;
CREATE TABLE IF NOT EXISTS "SupportThreads" (
  "Id" uuid PRIMARY KEY,
  "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "CaseId" uuid NULL REFERENCES "CaseTickets"("Id") ON DELETE RESTRICT,
  "CustodyId" uuid NULL REFERENCES "CustodyRecords"("Id") ON DELETE RESTRICT,
  "Department" varchar(20) NOT NULL,
  "CreatedAt" timestamptz NOT NULL,
  "LastMessageAt" timestamptz NOT NULL,
  CONSTRAINT "CK_SupportThreads_Source" CHECK (("CaseId" IS NOT NULL) <> ("CustodyId" IS NOT NULL)),
  CONSTRAINT "CK_SupportThreads_Department" CHECK ("Department" IN ('ADMIN','SECURITY','LIBRARY','DORM'))
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SupportThreads_CaseId" ON "SupportThreads"("CaseId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SupportThreads_CustodyId" ON "SupportThreads"("CustodyId");
CREATE INDEX IF NOT EXISTS "IX_SupportThreads_UserId_LastMessageAt" ON "SupportThreads"("UserId","LastMessageAt");

CREATE TABLE IF NOT EXISTS "SupportMessages" (
  "Id" uuid PRIMARY KEY,
  "ThreadId" uuid NOT NULL REFERENCES "SupportThreads"("Id") ON DELETE CASCADE,
  "SenderId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "SenderType" varchar(12) NOT NULL DEFAULT 'USER',
  "Content" varchar(1500) NOT NULL,
  "CreatedAt" timestamptz NOT NULL
);
ALTER TABLE "SupportMessages" ADD COLUMN IF NOT EXISTS "SenderType" varchar(12) NOT NULL DEFAULT 'USER';
CREATE INDEX IF NOT EXISTS "IX_SupportMessages_ThreadId_CreatedAt" ON "SupportMessages"("ThreadId","CreatedAt");

-- Existing claims/disputes and custody requests become accessible in Support.
-- Does not copy private evidence or create conversations between end users.
INSERT INTO "SupportThreads" ("Id","UserId","CaseId","CustodyId","Department","CreatedAt","LastMessageAt")
SELECT gen_random_uuid(), c."ReporterId", c."Id", NULL, 'ADMIN', c."CreatedAt", c."CreatedAt"
FROM "CaseTickets" c WHERE NOT EXISTS (SELECT 1 FROM "SupportThreads" s WHERE s."CaseId"=c."Id")
ON CONFLICT DO NOTHING;
INSERT INTO "SupportThreads" ("Id","UserId","CaseId","CustodyId","Department","CreatedAt","LastMessageAt")
SELECT gen_random_uuid(), c."RequestedById", NULL, c."Id",
 CASE WHEN c."Desk" ILIKE '%Thư viện%' THEN 'LIBRARY'
      WHEN c."Desk" ILIKE '%ký túc%' THEN 'DORM' ELSE 'SECURITY' END,
 c."CreatedAt", c."CreatedAt"
FROM "CustodyRecords" c WHERE NOT EXISTS (SELECT 1 FROM "SupportThreads" s WHERE s."CustodyId"=c."Id")
ON CONFLICT DO NOTHING;
