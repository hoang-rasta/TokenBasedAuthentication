using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceClient.DTOs
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } // Access Token
        public string RefreshToken { get; set; } // Refresh Token
    }
}
