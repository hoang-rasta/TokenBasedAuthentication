using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.Models
{
    // Tạo Index trên trường Token để đảm bảo không có hai refresh token nào có cùng chuỗi token
    [Index(nameof(Token), Name = "IX_Token_Unique", IsUnique = true)]
    public class RefreshToken
    {
        [Key] // Đánh dấu Id là khóa chính
        public int Id { get; set; }

        // Chuỗi refresh token (nên là một chuỗi ngẫu nhiên an toàn)
        [Required] // Yêu cầu phải có giá trị
        public string Token { get; set; }

        // Người dùng liên kết với refresh token
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")] // Định nghĩa khóa ngoại tới bảng Users
        public User User { get; set; } // Thuộc tính điều hướng đến đối tượng User

        // Client liên kết với refresh token
        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))] // Định nghĩa khóa ngoại tới bảng Clients
        public Client Client { get; set; } // Thuộc tính điều hướng đến đối tượng Client

        // Ngày hết hạn của token
        [Required]
        public DateTime ExpiresAt { get; set; }

        // Cho biết token đã bị thu hồi hay chưa (mặc định là false)
        [Required]
        public bool IsRevoked { get; set; } = false;

        // Ngày tạo token (mặc định là thời gian UTC hiện tại)
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Ngày token bị thu hồi (có thể null nếu chưa bị thu hồi)
        public DateTime? RevokedAt { get; set; }
    }
}
