-- Additive and safe for an existing database. Does not seed or fabricate reviews.
CREATE TABLE IF NOT EXISTS "FoundlyPublicReviews" (
  "Id" uuid PRIMARY KEY,
  "HandoverRoomId" uuid NOT NULL REFERENCES "HandoverRooms"("Id") ON DELETE RESTRICT,
  "ReviewerId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
  "Rating" integer NOT NULL CHECK ("Rating" BETWEEN 1 AND 5),
  "Comment" varchar(500) NOT NULL,
  "ParticipantRole" varchar(30) NOT NULL,
  "IsVisible" boolean NOT NULL DEFAULT true,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_FoundlyPublicReviews_HandoverRoomId_ReviewerId"
  ON "FoundlyPublicReviews" ("HandoverRoomId", "ReviewerId");
CREATE INDEX IF NOT EXISTS "IX_FoundlyPublicReviews_IsVisible_CreatedAt"
  ON "FoundlyPublicReviews" ("IsVisible", "CreatedAt" DESC);
