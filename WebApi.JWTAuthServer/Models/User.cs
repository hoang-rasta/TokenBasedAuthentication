using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.Models
{
    [Index(nameof(Email), Name = "IX_Unique_Email", IsUnique = true)]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Firstname { get; set; }

        public string? Lastname { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        //  Navigation property cho mối quan hệ nhiều-nhiều với Role thông qua bảng UserRole.
        public ICollection<UserRole> UserRoles { get; set; }
    }
}
