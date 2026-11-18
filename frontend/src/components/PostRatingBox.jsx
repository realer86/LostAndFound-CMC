import React, { useState, useEffect } from "react";

const API_BASE_URL = import.meta.env.VITE_API_URL || "/api";

export default function PostRatingBox({
  itemId,
  currentUser,
  myReports = [],
  reportOwnerId,
  reportType,
}) {
  const [ratings, setRatings] = useState([]);
  const [averageStars, setAverageStars] = useState(0);
  const [selectedStars, setSelectedStars] = useState(0);
  const [hoverStars, setHoverStars] = useState(0);
  const [comment, setComment] = useState("");
  const [selectedRole, setSelectedRole] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showAllRatings, setShowAllRatings] = useState(false);

  const displayName = currentUser?.fullName?.trim() || "";

  // Kiểm tra người dùng hiện tại đã có bài đăng LOST hoặc FOUND nào chưa
  const hasLostPost =
    myReports.some((r) => r.type === "LOST") ||
    (currentUser?.id && currentUser.id === reportOwnerId && reportType === "LOST");

  const hasFoundPost =
    myReports.some((r) => r.type === "FOUND") ||
    (currentUser?.id && currentUser.id === reportOwnerId && reportType === "FOUND");

  const canRate = Boolean(currentUser && displayName && (hasLostPost || hasFoundPost));

  // Tự động xác định vai trò hiển thị (Người tìm đồ / Người nhặt được)
  useEffect(() => {
    if (currentUser?.id && currentUser.id === reportOwnerId) {
      setSelectedRole(reportType === "FOUND" ? "FOUND" : "LOST");
    } else if (hasFoundPost && !hasLostPost) {
      setSelectedRole("FOUND");
    } else if (hasLostPost && !hasFoundPost) {
      setSelectedRole("LOST");
    } else if (hasLostPost && hasFoundPost && !selectedRole) {
      setSelectedRole("LOST");
    }
  }, [currentUser, reportOwnerId, reportType, hasLostPost, hasFoundPost]);

  // Tách tên và vai trò từ dữ liệu lưu trữ (ví dụ: "Realer [FOUND]")
  const parseReviewer = (rawName = "") => {
    if (rawName.endsWith("[FOUND]")) {
      return {
        name: rawName.replace("[FOUND]", "").trim(),
        role: "FOUND",
      };
    }
    if (rawName.endsWith("[LOST]")) {
      return {
        name: rawName.replace("[LOST]", "").trim(),
        role: "LOST",
      };
    }
    return { name: rawName, role: "LOST" };
  };

  const fetchRatings = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/items/${itemId}/ratings`);
      if (response.ok) {
        const data = await response.json();
        setRatings(data.ratings || []);
        setAverageStars(data.averageStars || 0);
        return;
      }
    } catch {}

    const localKey = `cmc_ratings_item_${itemId}`;
    const saved = JSON.parse(localStorage.getItem(localKey) || "[]");
    setRatings(saved);
    if (saved.length > 0) {
      const avg = saved.reduce((sum, r) => sum + r.stars, 0) / saved.length;
      setAverageStars(Number(avg.toFixed(1)));
    } else {
      setAverageStars(0);
    }
  };

  useEffect(() => {
    if (itemId) fetchRatings();
  }, [itemId]);

  const handleRateSubmit = async (e) => {
    e.preventDefault();
    if (!canRate || selectedStars < 1 || selectedStars > 5) return;

    setIsSubmitting(true);
    const roleTag = selectedRole === "FOUND" ? "[FOUND]" : "[LOST]";
    const taggedUserName = `${displayName} ${roleTag}`;

    const payload = {
      userName: taggedUserName,
      stars: selectedStars,
      comment: comment.trim(),
    };

    try {
      const response = await fetch(`${API_BASE_URL}/items/${itemId}/ratings`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        setComment("");
        setSelectedStars(0);
        await fetchRatings();
        setIsSubmitting(false);
        return;
      }
    } catch {}

    // Fallback LocalStorage
    const localKey = `cmc_ratings_item_${itemId}`;
    const saved = JSON.parse(localStorage.getItem(localKey) || "[]");
    const existingIndex = saved.findIndex((r) => {
      const parsed = parseReviewer(r.userName);
      return parsed.name.toLowerCase() === displayName.toLowerCase();
    });

    const newEntry = {
      id: Date.now(),
      itemId,
      userName: taggedUserName,
      stars: selectedStars,
      comment: comment.trim(),
      createdAt: new Date().toISOString(),
    };

    if (existingIndex >= 0) saved[existingIndex] = newEntry;
    else saved.unshift(newEntry);

    localStorage.setItem(localKey, JSON.stringify(saved));
    setComment("");
    setSelectedStars(0);
    await fetchRatings();
    setIsSubmitting(false);
  };

  const renderStars = (count) => "★".repeat(count) + "☆".repeat(5 - count);
  const activeStarCount = hoverStars || selectedStars;
  const starLabels = ["", "Chưa rõ", "Tạm được", "Hữu ích", "Rất tốt", "Tuyệt vời!"];

  return (
    <div style={styles.wrapper} onClick={(e) => e.stopPropagation()}>
      {/* Thanh tổng quan điểm sao */}
      <div style={styles.topBar}>
        <div style={styles.avgPill}>
          <span style={styles.avgStarIcon}>★</span>
          <span style={styles.avgScore}>
            {averageStars > 0 ? `${averageStars.toFixed(1)}/5` : "Chưa có"}
          </span>
          <span style={styles.avgCount}>({ratings.length} đánh giá)</span>
        </div>

        {canRate && (
          <div style={styles.userChip}>
            <span style={styles.userAvatar}>
              {displayName.charAt(0).toUpperCase()}
            </span>
            <span style={styles.userNameText}>{displayName}</span>
            <span
              style={
                selectedRole === "FOUND"
                  ? styles.roleBadgeFound
                  : styles.roleBadgeLost
              }
            >
              {selectedRole === "FOUND" ? "📦 Người nhặt được" : "🔍 Người tìm đồ"}
            </span>
          </div>
        )}
      </div>

      {/* Kiểm tra quyền: Chỉ người đã đăng tin LOST hoặc FOUND mới hiện form đánh giá */}
      {canRate ? (
        <form onSubmit={handleRateSubmit} style={styles.cardForm}>
          {/* Nếu người dùng có cả bài đăng Mất và bài Nhặt được thì cho phép chuyển đổi vai trò */}
          {hasLostPost && hasFoundPost && (
            <div style={styles.roleSwitchRow}>
              <span style={styles.roleSwitchLabel}>Đánh giá với tư cách:</span>
              <button
                type="button"
                onClick={() => setSelectedRole("LOST")}
                style={
                  selectedRole === "LOST"
                    ? styles.roleSwitchBtnLostActive
                    : styles.roleSwitchBtn
                }
              >
                🔍 Người tìm đồ
              </button>
              <button
                type="button"
                onClick={() => setSelectedRole("FOUND")}
                style={
                  selectedRole === "FOUND"
                    ? styles.roleSwitchBtnFoundActive
                    : styles.roleSwitchBtn
                }
              >
                📦 Người nhặt được
              </button>
            </div>
          )}

          <div style={styles.starRow}>
            <span style={styles.starLabel}>Chấm điểm:</span>
            <div style={styles.starsContainer}>
              {[1, 2, 3, 4, 5].map((star) => {
                const isFilled = star <= activeStarCount;
                return (
                  <button
                    key={star}
                    type="button"
                    onClick={() => setSelectedStars(star)}
                    onMouseEnter={() => setHoverStars(star)}
                    onMouseLeave={() => setHoverStars(0)}
                    style={{
                      ...styles.starBtn,
                      color: isFilled ? "#ff9f1c" : "#cbd5e1",
                      transform: star === hoverStars ? "scale(1.22)" : "scale(1)",
                    }}
                  >
                    ★
                  </button>
                );
              })}
            </div>
            {activeStarCount > 0 && (
              <span style={styles.ratingTag}>{starLabels[activeStarCount]}</span>
            )}
          </div>

          <div style={styles.commentBox}>
            <input
              type="text"
              placeholder="Viết nhận xét của bạn về bài đăng này..."
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              style={styles.commentInput}
              maxLength={160}
            />
            <button
              type="submit"
              disabled={isSubmitting || selectedStars === 0}
              style={{
                ...styles.submitBtn,
                opacity: isSubmitting || selectedStars === 0 ? 0.55 : 1,
                cursor:
                  isSubmitting || selectedStars === 0 ? "not-allowed" : "pointer",
              }}
            >
              Gửi
            </button>
          </div>
        </form>
      ) : (
        <div style={styles.restrictedNotice}>
          🔒 Chỉ thành viên đã đăng tin <strong>Mất đồ</strong> hoặc{" "}
          <strong>Nhặt được đồ</strong> mới có thể đánh giá bài đăng.
        </div>
      )}

      {/* Danh sách đánh giá hiển thị rõ Tên + Nhãn Người tìm / Người nhặt được */}
      {ratings.length > 0 && (
        <div style={styles.reviewSection}>
          <div style={styles.reviewList}>
            {(showAllRatings ? ratings : ratings.slice(0, 2)).map((item) => {
              const parsed = parseReviewer(item.userName);
              const isFinder = parsed.role === "FOUND";
              return (
                <div key={item.id} style={styles.reviewCard}>
                  <div style={styles.reviewHeader}>
                    <div style={styles.reviewerBadge}>
                      <span style={styles.reviewerInitial}>
                        {parsed.name.charAt(0).toUpperCase()}
                      </span>
                      <strong style={styles.reviewerName}>{parsed.name}</strong>
                      <span
                        style={
                          isFinder ? styles.roleBadgeFound : styles.roleBadgeLost
                        }
                      >
                        {isFinder ? "📦 Người nhặt được" : "🔍 Người tìm đồ"}
                      </span>
                    </div>
                    <span style={styles.reviewStars}>
                      {renderStars(item.stars)}
                    </span>
                  </div>
                  {item.comment && (
                    <p style={styles.reviewComment}>{item.comment}</p>
                  )}
                </div>
              );
            })}
          </div>

          {ratings.length > 2 && (
            <button
              type="button"
              onClick={() => setShowAllRatings(!showAllRatings)}
              style={styles.moreBtn}
            >
              {showAllRatings
                ? "Thu gọn đánh giá ▲"
                : `Xem tất cả ${ratings.length} đánh giá ▼`}
            </button>
          )}
        </div>
      )}
    </div>
  );
}

const styles = {
  wrapper: {
    marginTop: "8px",
    padding: "12px 14px",
    background: "linear-gradient(145deg, #f0fdf9 0%, #ecfeff 100%)",
    borderRadius: "14px",
    border: "1px solid #99f6e4",
    boxShadow: "0 4px 12px rgba(13, 148, 136, 0.07)",
  },
  topBar: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    gap: "8px",
    marginBottom: "10px",
    flexWrap: "wrap",
  },
  avgPill: {
    display: "inline-flex",
    alignItems: "center",
    gap: "5px",
    background: "linear-gradient(135deg, #fef3c7 0%, #fde68a 100%)",
    padding: "4px 10px",
    borderRadius: "999px",
    border: "1px solid #fcd34d",
  },
  avgStarIcon: {
    color: "#d97706",
    fontSize: "14px",
    lineHeight: 1,
  },
  avgScore: {
    fontWeight: "700",
    fontSize: "12.5px",
    color: "#92400e",
  },
  avgCount: {
    fontSize: "11.5px",
    color: "#b45309",
  },
  userChip: {
    display: "inline-flex",
    alignItems: "center",
    gap: "6px",
    backgroundColor: "#ffffff",
    padding: "3px 8px 3px 4px",
    borderRadius: "999px",
    border: "1px solid #99f6e4",
  },
  userAvatar: {
    width: "21px",
    height: "21px",
    borderRadius: "50%",
    background: "linear-gradient(135deg, #0d9488 0%, #10b981 100%)",
    color: "#ffffff",
    fontWeight: "700",
    fontSize: "11px",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
  },
  userNameText: {
    fontSize: "12px",
    fontWeight: "700",
    color: "#0f766e",
  },
  roleBadgeFound: {
    fontSize: "10.5px",
    fontWeight: "700",
    backgroundColor: "#d1fae5",
    color: "#065f46",
    border: "1px solid #6ee7b7",
    padding: "2px 7px",
    borderRadius: "999px",
    whiteSpace: "nowrap",
  },
  roleBadgeLost: {
    fontSize: "10.5px",
    fontWeight: "700",
    backgroundColor: "#ffedd5",
    color: "#9a3412",
    border: "1px solid #fdba74",
    padding: "2px 7px",
    borderRadius: "999px",
    whiteSpace: "nowrap",
  },
  roleSwitchRow: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    flexWrap: "wrap",
    paddingBottom: "6px",
    borderBottom: "1px dashed #e2e8f0",
  },
  roleSwitchLabel: {
    fontSize: "11.5px",
    color: "#64748b",
    fontWeight: "600",
  },
  roleSwitchBtn: {
    fontSize: "11px",
    padding: "3px 8px",
    borderRadius: "999px",
    border: "1px solid #cbd5e1",
    backgroundColor: "#f8fafc",
    color: "#475569",
    cursor: "pointer",
    fontWeight: "600",
  },
  roleSwitchBtnLostActive: {
    fontSize: "11px",
    padding: "3px 8px",
    borderRadius: "999px",
    border: "1px solid #f97316",
    backgroundColor: "#ffedd5",
    color: "#9a3412",
    cursor: "pointer",
    fontWeight: "700",
  },
  roleSwitchBtnFoundActive: {
    fontSize: "11px",
    padding: "3px 8px",
    borderRadius: "999px",
    border: "1px solid #10b981",
    backgroundColor: "#d1fae5",
    color: "#065f46",
    cursor: "pointer",
    fontWeight: "700",
  },
  cardForm: {
    display: "flex",
    flexDirection: "column",
    gap: "8px",
    backgroundColor: "#ffffff",
    padding: "10px 12px",
    borderRadius: "12px",
    border: "1px solid #cbd5e1",
  },
  starRow: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    flexWrap: "wrap",
  },
  starLabel: {
    fontSize: "12.5px",
    fontWeight: "600",
    color: "#334155",
  },
  starsContainer: {
    display: "flex",
    alignItems: "center",
    gap: "2px",
  },
  starBtn: {
    background: "none",
    border: "none",
    fontSize: "21px",
    cursor: "pointer",
    padding: "0 2px",
    lineHeight: 1,
    transition: "transform 0.15s ease, color 0.15s ease",
  },
  ratingTag: {
    marginLeft: "auto",
    fontSize: "11.5px",
    fontWeight: "700",
    color: "#0d9488",
    backgroundColor: "#ccfbf1",
    padding: "2px 8px",
    borderRadius: "999px",
  },
  commentBox: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
  },
  commentInput: {
    flex: 1,
    minWidth: 0,
    padding: "7px 11px",
    borderRadius: "999px",
    border: "1px solid #cbd5e1",
    fontSize: "12.5px",
    color: "#1e293b",
    backgroundColor: "#f8fafc",
    outline: "none",
  },
  submitBtn: {
    padding: "7px 15px",
    background: "linear-gradient(135deg, #0d9488 0%, #10b981 100%)",
    color: "#ffffff",
    border: "none",
    borderRadius: "999px",
    fontWeight: "700",
    fontSize: "12.5px",
    boxShadow: "0 2px 6px rgba(13, 148, 136, 0.25)",
    whiteSpace: "nowrap",
  },
  restrictedNotice: {
    backgroundColor: "#ffffff",
    border: "1px dashed #99f6e4",
    borderRadius: "10px",
    padding: "8px 10px",
    fontSize: "12px",
    color: "#0f766e",
    textAlign: "center",
  },
  reviewSection: {
    marginTop: "10px",
    display: "flex",
    flexDirection: "column",
    gap: "6px",
  },
  reviewList: {
    display: "flex",
    flexDirection: "column",
    gap: "6px",
  },
  reviewCard: {
    backgroundColor: "#ffffff",
    borderRadius: "10px",
    padding: "8px 10px",
    border: "1px solid #e2e8f0",
  },
  reviewHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    flexWrap: "wrap",
    gap: "4px",
  },
  reviewerBadge: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    flexWrap: "wrap",
  },
  reviewerInitial: {
    width: "20px",
    height: "20px",
    borderRadius: "50%",
    backgroundColor: "#ccfbf1",
    color: "#0f766e",
    fontWeight: "700",
    fontSize: "11px",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
  },
  reviewerName: {
    fontSize: "12.5px",
    color: "#0f172a",
  },
  reviewStars: {
    color: "#f59e0b",
    fontSize: "12px",
    letterSpacing: "1px",
  },
  reviewComment: {
    margin: "4px 0 0 26px",
    fontSize: "12px",
    color: "#475569",
    lineHeight: 1.4,
  },
  moreBtn: {
    alignSelf: "center",
    background: "none",
    border: "none",
    color: "#0d9488",
    fontSize: "12px",
    fontWeight: "600",
    cursor: "pointer",
    padding: "2px 6px",
  },
};