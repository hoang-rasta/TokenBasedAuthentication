using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.Models
{
    //SigningKey để lưu trữ thông tin Key nhằm quản lý các khóa RSA
    public class SigningKey
    {
        [Key]
        public int Id { get; set; }

        // Mã định danh duy nhất cho khóa (Key ID).
        [Required]
        [MaxLength(100)]
        public string KeyId { get; set; }

        // Khóa riêng tư RSA.
        [Required]
        public string PrivateKey { get; set; }

        // Khóa công khai RSA ở định dạng XML hoặc PEM.
        [Required]
        public string PublicKey { get; set; }

        // Cho biết khóa có đang hoạt động hay không.
        [Required]
        public bool IsActive { get; set; }

        // Ngày tạo khóa.
        [Required]
        public DateTime CreatedAt { get; set; }

        // Ngày khóa hết hạn.
        [Required]
        public DateTime ExpiresAt { get; set; }
    }
}
