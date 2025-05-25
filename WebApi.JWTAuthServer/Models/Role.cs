using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.Models
{
    public class Role
    {
        // Khóa chính cho thực thể Role.
        [Key]
        public int Id { get; set; }

        // Tên của Role (ví dụ: Admin, User).
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        // Mô tả Role.
        public string? Description { get; set; }

        // Navigation property cho mối quan hệ với UserRole (để truy cập các UserRole liên quan).
        public ICollection<UserRole> UserRoles { get; set; }
    }
}
