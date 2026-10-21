-- Foundly #13 (Sprint 2) và #17 (Sprint 3)
-- NÂNG CẤP KHÔNG PHÁ HỦY: giữ nguyên bảng/dữ liệu của Sprint 1.
-- PostgreSQL 17. Tên bảng/cột khớp EF Core 10. Có thể chạy lại.
-- Khi Deploy: sao lưu dữ liệu trước khi nâng cấp.
BEGIN;

CREATE TABLE IF NOT EXISTS "OfficeDirectory" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "Type" varchar(24) NOT NULL,
  "Name" varchar(120) NOT NULL,
  "Location" varchar(200) NOT NULL,
  "InternalPhone" varchar(30) NOT NULL,
  "WorkingSchedule" varchar(350) NOT NULL,
  "DutiesByTime" varchar(600) NOT NULL,
  CONSTRAINT "CK_OfficeDirectory_Type" CHECK ("Type" IN ('SECURITY','MANAGEMENT'))
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_OfficeDirectory_Name" ON "OfficeDirectory" ("Name");

CREATE TABLE IF NOT EXISTS "Messages" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ReportId" uuid NOT NULL,
  "SenderId" uuid NOT NULL,
  "RecipientId" uuid NOT NULL,
  "Content" varchar(2000) NOT NULL,
  "CreatedAt" timestamptz NOT NULL,
  CONSTRAINT "CK_Messages_DifferentUsers" CHECK ("SenderId" <> "RecipientId"),
  CONSTRAINT "FK_Messages_Reports_ReportId" FOREIGN KEY ("ReportId") REFERENCES "Reports"("Id") ON DELETE RESTRICT,
  CONSTRAINT "FK_Messages_Users_SenderId" FOREIGN KEY ("SenderId") REFERENCES "Users"("Id") ON DELETE RESTRICT,
  CONSTRAINT "FK_Messages_Users_RecipientId" FOREIGN KEY ("RecipientId") REFERENCES "Users"("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_Messages_ReportId_CreatedAt" ON "Messages" ("ReportId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Messages_RecipientId_CreatedAt" ON "Messages" ("RecipientId", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Messages_SenderId" ON "Messages" ("SenderId");

CREATE TABLE IF NOT EXISTS "Claims" (
  "Id" uuid NOT NULL PRIMARY KEY,
  "ItemId" uuid NOT NULL,
  "ClaimerId" uuid NOT NULL,
  "SecretText" varchar(4000) NOT NULL,
  "Status" varchar(16) NOT NULL DEFAULT 'PENDING',
  "CreatedAt" timestamptz NOT NULL,
  "ReviewedAt" timestamptz NULL,
  CONSTRAINT "CK_Claims_Status" CHECK ("Status" IN ('PENDING','APPROVED','REJECTED')),
  CONSTRAINT "FK_Claims_Reports_ItemId" FOREIGN KEY ("ItemId") REFERENCES "Reports"("Id") ON DELETE RESTRICT,
  CONSTRAINT "FK_Claims_Users_ClaimerId" FOREIGN KEY ("ClaimerId") REFERENCES "Users"("Id") ON DELETE RESTRICT
);
-- 1 người chỉ gửi 1 yêu cầu cho mỗi bài, không spam trùng.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Claims_ItemId_ClaimerId" ON "Claims" ("ItemId", "ClaimerId");
CREATE INDEX IF NOT EXISTS "IX_Claims_ClaimerId" ON "Claims" ("ClaimerId");
-- Chặn tình huống duyệt đồng thời 2 người ở mức DATABASE.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Claims_OnlyOneApproved" ON "Claims" ("ItemId") WHERE "Status" = 'APPROVED';

COMMIT;
