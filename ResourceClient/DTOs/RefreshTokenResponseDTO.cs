using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceClient.DTOs
{
    public class RefreshTokenResponseDTO
    {
        public string Token { get; set; } // Access Token mới
        public string RefreshToken { get; set; } // Refresh Token mới (tùy chọn)
    }
}
