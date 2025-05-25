using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.Models
{
    //This class represents a client that can request tokens from the auth server, typically identified by the ClientId
    public class Client
    {
        [Key]
        public int Id { get; set; }

        // Mã định danh duy nhất cho client application.
        [Required]
        [MaxLength(100)]
        public string ClientId { get; set; }

        // Tên của client application.
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        // URL của client application.
        [Required]
        [MaxLength(200)]
        public string ClientURL { get; set; }

        // Navigation property cho refresh tokens
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>(); // Thêm dòng này và khởi tạo
    }
}
