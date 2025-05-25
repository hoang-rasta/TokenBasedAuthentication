using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.DTOs
{
    public class RefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; }

        [Required]
        public string ClientId { get; set; }
    }
}
