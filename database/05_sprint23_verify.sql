-- Chạy sau khi Backend đã boot: dùng DBeaver hoặc psql ở DB test.
-- Các câu SELECT chỉ kiểm tra cấu trúc/trạng thái, không tiết lộ SecretText.
SELECT table_name FROM information_schema.tables
 WHERE table_schema = 'public' AND table_name IN ('Claims','Messages','OfficeDirectory')
 ORDER BY table_name;

SELECT column_name, data_type, is_nullable FROM information_schema.columns
 WHERE table_schema = 'public' AND table_name = 'Claims' ORDER BY ordinal_position;

SELECT "ItemId", "Status", COUNT(*) AS total FROM "Claims"
 GROUP BY "ItemId", "Status" ORDER BY "ItemId", "Status";

-- Phải trả về 0 hàng: không được có hai yêu cầu APPROVED trên cùng bài.
SELECT "ItemId", COUNT(*) FROM "Claims" WHERE "Status" = 'APPROVED'
 GROUP BY "ItemId" HAVING COUNT(*) > 1;

-- Chỉ số điện thoại NỘI BỘ được lưu tại OfficeDirectory, không lưu SĐT cá nhân.
SELECT "Type", "Name", "Location", "InternalPhone", "WorkingSchedule", "DutiesByTime"
 FROM "OfficeDirectory" ORDER BY "Type", "Name";
