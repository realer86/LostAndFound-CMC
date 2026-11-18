using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LostFound.Api.Domain
{
    public class PostRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; } // Khóa ngoại trỏ tới bài đăng (Item)

        [Required]
        public string UserName { get; set; } = string.Empty; // Tên hiển thị của người dùng đánh giá

        public string? UserId { get; set; } // (Tùy chọn) ID người dùng nếu lấy từ Token

        [Required]
        [Range(1, 5, ErrorMessage = "Số sao đánh giá phải từ 1 đến 5.")]
        public int Stars { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; } // Nhận xét ngắn (tùy chọn)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}