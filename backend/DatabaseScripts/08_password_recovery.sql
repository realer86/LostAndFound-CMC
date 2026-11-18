-- Add-only, repeatable migration: preserves existing users and OTP/email verification state.
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SessionVersion" integer NOT NULL DEFAULT 0;
CREATE TABLE IF NOT EXISTS "PasswordResetTickets" (
    "UserId" uuid NOT NULL PRIMARY KEY REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash" varchar(200) NULL,
    "ExpiresAt" timestamp with time zone NULL,
    "SentAt" timestamp with time zone NULL,
    "FailedAttempts" integer NOT NULL DEFAULT 0,
    "RequestWindowStartedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "RequestsInWindow" integer NOT NULL DEFAULT 0,
    "DailyWindowStartedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "RequestsToday" integer NOT NULL DEFAULT 0,
    "FailedWindowStartedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "FailedAttemptsInWindow" integer NOT NULL DEFAULT 0
);
