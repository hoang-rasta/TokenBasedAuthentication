using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace WebApi.JWTAuthServer.Models
{
    public class UserRole
    {
        // Khóa ngoại tham chiếu đến User.
        public int UserId { get; set; }

        // Thuộc tính điều hướng đến User.
        public User User { get; set; }

        // Khóa ngoại tham chiếu đến Role.
        public int RoleId { get; set; }

        // Thuộc tính điều hướng đến Role.
        public Role Role { get; set; }
    }
}