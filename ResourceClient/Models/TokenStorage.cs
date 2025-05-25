using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceClient.Models
{
    public class TokenStorage
    {
        // Lưu trữ access token hiện tại được sử dụng cho các yêu cầu API đã xác thực.
        public string AccessToken { get; set; } = string.Empty;
        // Lưu trữ refresh token hiện tại được sử dụng để lấy các access token mới.
        public string RefreshToken { get; set; } = string.Empty;
        // Lưu trữ định danh client liên kết với các token.
        public string ClientId { get; set; } = string.Empty;
    }
}
