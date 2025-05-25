using Microsoft.AspNetCore.Mvc; 
using Microsoft.IdentityModel.Tokens; 
using System.Security.Cryptography; 

namespace WebApi.JWTAuthServer.Controllers
{
    // Đặt route cho controller này. Controller này cụ thể sẽ công khai các khóa
    // tại các đường dẫn như "/.well-known/jwks.json" để tuân theo các tiêu chuẩn chung.
    [Route(".well-known")]
    [ApiController] 
    public class JWKSController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JWKSController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Định nghĩa endpoint JWKS. Nó phản hồi các yêu cầu GET tại '/.well-known/jwks.json'.
        [HttpGet("jwks.json")]
        public IActionResult GetJWKS()
        {
            // Truy xuất tất cả các khóa ký đang hoạt động từ cơ sở dữ liệu.
            // Chỉ các khóa đang hoạt động mới nên được công khai để xác minh.
            var keys = _context.SigningKeys.Where(k => k.IsActive).ToList();

            // Xây dựng đối tượng JWKS (JSON Web Key Set).
            var jwks = new
            {
                // Thuộc tính 'keys' là một mảng các JSON Web Key (JWK) riêng lẻ.
                keys = keys.Select(k => new // Với mỗi khóa ký đang hoạt động, tạo một JWK.
                {
                    kty = "RSA",    // Kiểu khóa: RSA cho các khóa mã hóa RSA.
                    use = "sig",    // Mục đích sử dụng: "sig" chỉ ra khóa được dùng để xác minh chữ ký.
                    kid = k.KeyId,  // ID Khóa: Định danh duy nhất cho khóa cụ thể này. Điều này giúp các bên tiêu thụ
                                    // chọn đúng khóa công khai khi có nhiều khóa khả dụng.
                    alg = "RS256",  // Thuật toán: RS256 (Chữ ký RSA với SHA-256) được dùng để ký.
                    // Modulus: Trích xuất môđun từ khóa công khai, sau đó mã hóa Base64URL.
                    n = Base64UrlEncoder.Encode(GetModulus(k.PublicKey)),
                    // Exponent: Trích xuất số mũ công khai từ khóa công khai, sau đó mã hóa Base64URL.
                    e = Base64UrlEncoder.Encode(GetExponent(k.PublicKey))
                })
            };

            // Trả về đối tượng JWKS đã xây dựng dưới dạng phản hồi JSON với mã trạng thái HTTP 200 OK.
            return Ok(jwks);
        }

        // Phương thức trợ giúp để trích xuất thành phần môđun từ một chuỗi khóa công khai được mã hóa Base64.
        private byte[] GetModulus(string publicKey)
        {
            // Tạo một thể hiện RSA mới cho các hoạt động mã hóa.
            var rsa = RSA.Create();
            // Import khóa công khai RSA từ biểu diễn chuỗi được mã hóa Base64 của nó.
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            // Export các tham số RSA (môđun và số mũ) mà không bao gồm khóa riêng tư.
            var parameters = rsa.ExportParameters(false);
            // Giải phóng thể hiện RSA để giải phóng tài nguyên và ngăn rò rỉ bộ nhớ.
            rsa.Dispose();
            // Kiểm tra xem môđun có null không, điều này cho thấy các tham số RSA không hợp lệ.
            if (parameters.Modulus == null)
            {
                throw new InvalidOperationException("RSA parameters are not valid.");
            }
            // Trả về thành phần môđun của khóa RSA.
            return parameters.Modulus;
        }

        // Phương thức trợ giúp để trích xuất thành phần số mũ từ một chuỗi khóa công khai được mã hóa Base64.
        private byte[] GetExponent(string publicKey)
        {
            // Tạo một thể hiện RSA mới cho các hoạt động mã hóa.
            var rsa = RSA.Create();
            // Import khóa công khai RSA từ biểu diễn chuỗi được mã hóa Base64 của nó.
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            // Export các tham số RSA mà không bao gồm khóa riêng tư.
            var parameters = rsa.ExportParameters(false);
            // Giải phóng thể hiện RSA.
            rsa.Dispose();
            // Kiểm tra xem số mũ có null không.
            if (parameters.Exponent == null)
            {
                throw new InvalidOperationException("RSA parameters are not valid.");
            }
            // Trả về thành phần số mũ của khóa RSA.
            return parameters.Exponent;
        }
    }
}
