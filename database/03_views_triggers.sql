-- Các VIEW và TRIGGER hỗ trợ Issue #1, chạy được nhiều lần.
-- Trong Sprint 1, RETURNED thay đổi bằng API của chủ bài; xác minh 2 bên thuộc bản Final.
CREATE OR REPLACE VIEW "vw_ReportCatalog" AS
SELECT r."Id", r."ItemName", r."Type", r."Status", r."CategoryId", c."Name" AS "CategoryName",
       r."LocationId", l."Name" AS "LocationName", r."CreatedAt"
FROM "Reports" r
LEFT JOIN "Categories" c ON r."CategoryId" = c."Id"
LEFT JOIN "Locations" l ON r."LocationId" = l."Id";

CREATE OR REPLACE VIEW "vw_OpenReports" AS
SELECT "Id", "ItemName", "Type", "Status", "CategoryName", "LocationName", "CreatedAt"
FROM "vw_ReportCatalog" WHERE "Status" = 'OPEN';

-- Update timestamp ở mức cơ sở dữ liệu, kể cả khi sửa trực tiếp bằng SQL.
CREATE OR REPLACE FUNCTION fn_reports_touch_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  NEW."UpdatedAt" = now();
  RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_reports_touch_updated_at ON "Reports";
CREATE TRIGGER trg_reports_touch_updated_at
BEFORE UPDATE ON "Reports"
FOR EACH ROW EXECUTE FUNCTION fn_reports_touch_updated_at();
