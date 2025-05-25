using System.ComponentModel.DataAnnotations;

namespace WebApi.JWTAuthServer.DTOs
{
    public class LogoutRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; }

        [Required]
        public string ClientId { get; set; }

        public bool IsLogoutFromAllDevices { get; set; }
    }
}
