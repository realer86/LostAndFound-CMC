-- Add-only migration: safe for an existing Foundly PostgreSQL volume.
-- Run after EnsureCreated; do not drop users, reports, contacts, or messages.
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "OtpSentAt" timestamp with time zone NULL;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "OtpAttemptCount" integer NOT NULL DEFAULT 0;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "GoogleSubject" varchar(128) NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_GoogleSubject" ON "Users"("GoogleSubject");
