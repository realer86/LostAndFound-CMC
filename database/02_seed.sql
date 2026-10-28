-- Idempotent seed (độc lập Categories/Locations); user mẫu do Backend tạo,
-- không đưa mật khẩu plain text vào script SQL.
INSERT INTO "Categories"("Name")
SELECT v.name FROM (VALUES ('Điện tử'),('Giấy tờ'),('Ví & túi'),('Chìa khóa'),('Trang sức'),('Sách & đồ dùng'),('Khác')) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM "Categories" c WHERE c."Name"=v.name);

INSERT INTO "Locations"("Name")
SELECT v.name FROM (VALUES ('Thư viện'),('Giảng đường A'),('Giảng đường B'),('Căn tin'),('Ký túc xá'),('Sân thể thao'),('Bãi xe'),('Khu vực khác')) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM "Locations" l WHERE l."Name"=v.name);
