-- Additive, idempotent schema for Claims, custody, handover, notifications and audit.
-- This file MUST NOT drop old Foundly tables or any existing data.
-- Legacy Foundly Pro used a different "Claims" table (ItemId, ClaimerId,
-- SecretText). Never reuse or alter it for the report-based Claims feature.
-- The report-based workflow intentionally uses "ReportClaims" instead.
-- Existing legacy claim records remain intact; they are NOT automatically
-- migrated into the new report-based workflow.
ALTER TABLE "CaseTickets" ADD COLUMN IF NOT EXISTS "RoomId" uuid NULL;
ALTER TABLE "CaseTickets" ADD COLUMN IF NOT EXISTS "Resolution" varchar(1500) NULL;
ALTER TABLE "CaseTickets" ADD COLUMN IF NOT EXISTS "ResolvedById" uuid NULL;
ALTER TABLE "CaseTickets" ADD COLUMN IF NOT EXISTS "ResolvedAt" timestamptz NULL;

CREATE TABLE IF NOT EXISTS "ReportClaims" (
  "Id" uuid PRIMARY KEY,
  "FoundReportId" uuid NOT NULL REFERENCES "Reports"("Id") ON DELETE RESTRICT,
  "ClaimantId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "Evidence" varchar(4000) NOT NULL,
  "Status" varchar(16) NOT NULL,
  "CreatedAt" timestamptz NOT NULL,
  "ReviewedAt" timestamptz NULL,
  "HandedOverAt" timestamptz NULL
);
CREATE INDEX IF NOT EXISTS "IX_ReportClaims_FoundReportId_ClaimantId" ON "ReportClaims"("FoundReportId","ClaimantId");

CREATE TABLE IF NOT EXISTS "HandoverRooms" (
  "Id" uuid PRIMARY KEY,
  "ClaimId" uuid NOT NULL UNIQUE REFERENCES "ReportClaims"("Id") ON DELETE RESTRICT,
  "Status" varchar(24) NOT NULL,
  "HolderConfirmedAt" timestamptz NULL,
  "ClaimantConfirmedAt" timestamptz NULL,
  "CompletedAt" timestamptz NULL,
  "CreatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_HandoverRooms_ClaimId" ON "HandoverRooms"("ClaimId");

CREATE TABLE IF NOT EXISTS "HandoverMessages" (
  "Id" uuid PRIMARY KEY,
  "RoomId" uuid NOT NULL REFERENCES "HandoverRooms"("Id") ON DELETE CASCADE,
  "SenderId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "Content" varchar(1500) NOT NULL,
  "CreatedAt" timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_HandoverMessages_RoomId_CreatedAt" ON "HandoverMessages"("RoomId","CreatedAt");

CREATE TABLE IF NOT EXISTS "HandoverAppointments" (
  "Id" uuid PRIMARY KEY,
  "RoomId" uuid NOT NULL REFERENCES "HandoverRooms"("Id") ON DELETE CASCADE,
  "ProposedById" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "Place" varchar(120) NOT NULL,
  "MeetingAt" timestamptz NOT NULL,
  "Status" varchar(20) NOT NULL,
  "CreatedAt" timestamptz NOT NULL,
  "RespondedAt" timestamptz NULL,
  "ReminderSentAt" timestamptz NULL
);
CREATE INDEX IF NOT EXISTS "IX_HandoverAppointments_RoomId_CreatedAt" ON "HandoverAppointments"("RoomId","CreatedAt");

CREATE TABLE IF NOT EXISTS "Notifications" (
  "Id" uuid PRIMARY KEY,
  "UserId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "Type" varchar(60) NOT NULL,
  "Content" varchar(500) NOT NULL,
  "ReportId" uuid NULL,
  "IsRead" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_CreatedAt" ON "Notifications"("UserId","CreatedAt");

CREATE TABLE IF NOT EXISTS "AuditLogs" (
  "Id" uuid PRIMARY KEY,
  "ActorId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "Action" varchar(70) NOT NULL,
  "TargetType" varchar(70) NOT NULL,
  "TargetId" uuid NOT NULL,
  "CreatedAt" timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS "CustodyRecords" (
  "Id" uuid PRIMARY KEY,
  "ReportId" uuid NOT NULL UNIQUE REFERENCES "Reports"("Id") ON DELETE RESTRICT,
  "RequestedById" uuid NOT NULL,
  "Desk" varchar(120) NOT NULL,
  "Status" varchar(24) NOT NULL,
  "ReceiptCode" varchar(120) NULL,
  "AcceptedByAdminId" uuid NULL,
  "ReceivedAt" timestamptz NULL,
  "ReleasedAt" timestamptz NULL,
  "CreatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CustodyRecords_ReportId" ON "CustodyRecords"("ReportId");
