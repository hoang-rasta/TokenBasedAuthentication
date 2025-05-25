using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceClient.DTOs
{
    public class RefreshTokenRequestDTO
    {
        public string RefreshToken { get; set; }
        public string ClientId { get; set; }
    }
}
