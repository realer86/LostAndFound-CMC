import React, { useState } from "react";

const API_BASE_URL = import.meta.env.VITE_API_URL || "/api";

export default function ExportReturnedItemsButton({ items = [] }) {
  const [isExporting, setIsExporting] = useState(false);

  const isReturnedStatus = (status) => {
    if (!status) return false;
    const normalized = status.toString().toLowerCase().trim();
    return (
      normalized === "returned" ||
      normalized === "completed" ||
      normalized === "handedover" ||
      normalized === "handed_over" ||
      normalized === "claimed" ||
      normalized === "closed" ||
      normalized.includes("đã trả") ||
      normalized.includes("đã nhận") ||
      normalized.includes("hoàn tất")
    );
  };

  const exportFromClientList = (returnedList) => {
    const headers = [
      "STT",
      "Mã Đồ Vật (ID)",
      "Tên Món Đồ",
      "Danh Mục",
      "Vị Trí",
      "Loại Tin",
      "Trạng Thái",
      "Ngày Ghi Nhận",
    ];

    const escapeCsv = (val) => {
      if (val === null || val === undefined) return '""';
      const str = String(val).replace(/"/g, '""');
      return `"${str}"`;
    };

    const rows = returnedList.map((item, index) => [
      index + 1,
      escapeCsv(item.id || item.itemId || ""),
      escapeCsv(item.itemName || item.title || item.name || "Không tên"),
      escapeCsv(item.category || item.categoryName || "Vật dụng khác"),
      escapeCsv(item.location || item.locationDetail || "Trường ĐH CMC"),
      escapeCsv(item.type === "FOUND" ? "Đồ nhặt được" : "Đồ bị mất"),
      escapeCsv("Đã trao trả"),
      escapeCsv(
        item.updatedAt || item.returnedAt || item.createdAt
          ? new Date(
              item.updatedAt || item.returnedAt || item.createdAt
            ).toLocaleString("vi-VN")
          : new Date().toLocaleDateString("vi-VN")
      ),
    ]);

    const csvContent =
      "\uFEFF" +
      [headers.join(","), ...rows.map((e) => e.join(","))].join("\n");

    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    const dateStr = new Date().toISOString().slice(0, 10);

    link.href = url;
    link.setAttribute("download", `ThongKe_DoDaTra_CMC_${dateStr}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const handleExport = async () => {
    setIsExporting(true);

    const returnedFromProps = Array.isArray(items)
      ? items.filter((item) => isReturnedStatus(item.status))
      : [];

    if (returnedFromProps.length > 0) {
      exportFromClientList(returnedFromProps);
      setIsExporting(false);
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/items/export-returned`, {
        credentials: "same-origin",
      });
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `ThongKe_DoDaTra_CMC_${new Date().toISOString().slice(0, 10)}.csv`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
        setIsExporting(false);
        return;
      }
    } catch {}

    alert("Hiện chưa có món đồ nào ở trạng thái 'Đã trả' để xuất file thống kê!");
    setIsExporting(false);
  };

  const returnedCount = Array.isArray(items)
    ? items.filter((item) => isReturnedStatus(item.status)).length
    : 0;

  return (
    <button
      type="button"
      onClick={handleExport}
      disabled={isExporting}
      className="btn primary"
      style={styles.buttonOverride}
      title="Chỉ dành cho Admin: Xuất danh sách các món đồ đã trao trả ra file Excel/CSV"
    >
      <svg
        width="16"
        height="16"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
        <polyline points="7 10 12 15 17 10" />
        <line x1="12" y1="15" x2="12" y2="3" />
      </svg>
      <span>{isExporting ? "Đang xuất..." : "Xuất file đồ đã trả"}</span>
      {returnedCount > 0 && <span style={styles.badge}>{returnedCount}</span>}
    </button>
  );
}

const styles = {
  buttonOverride: {
    display: "inline-flex",
    alignItems: "center",
    gap: "8px",
    padding: "9px 16px",
    background: "linear-gradient(135deg, #0d9488 0%, #10b981 100%)",
    color: "#ffffff",
    border: "none",
    borderRadius: "999px",
    fontWeight: "700",
    fontSize: "13.5px",
    cursor: "pointer",
    boxShadow: "0 2px 8px rgba(13, 148, 136, 0.22)",
  },
  badge: {
    backgroundColor: "rgba(255, 255, 255, 0.24)",
    color: "#ffffff",
    padding: "1px 8px",
    borderRadius: "999px",
    fontSize: "12px",
    fontWeight: "700",
  },
};