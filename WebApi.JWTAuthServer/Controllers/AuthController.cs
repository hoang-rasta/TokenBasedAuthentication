using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using WebApi.JWTAuthServer.DTOs;
using WebApi.JWTAuthServer.Models;

namespace WebApi.JWTAuthServer.Controllers
{
    // Định nghĩa route cho controller này. Mọi endpoint sẽ bắt đầu bằng "api/Auth".
    [Route("api/[controller]")]
    // Thuộc tính chỉ ra đây là một API Controller, tự động xử lý một số tác vụ như validation.
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Trường private chỉ đọc để lưu trữ cấu hình ứng dụng (ví dụ: từ appsettings.json hoặc biến môi trường).
        private readonly IConfiguration _configuration;

        // Trường private chỉ đọc để lưu trữ đối tượng DbContext, giúp tương tác với cơ sở dữ liệu.
        private readonly ApplicationDbContext _context;

        // Constructor: Sử dụng Dependency Injection để inject IConfiguration và ApplicationDbContext.
        public AuthController(IConfiguration configuration, ApplicationDbContext context)
        {
            // Gán IConfiguration đã được inject vào trường _configuration.
            _configuration = configuration;

            // Gán ApplicationDbContext đã được inject vào trường _context.
            _context = context;
        }

        // ---

        // Định nghĩa endpoint Login, chấp nhận các yêu cầu POST tại 'api/Auth/Login'.
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            // Bước 1: Xác thực dữ liệu đầu vào.
            // Kiểm tra xem mô hình LoginDTO có hợp lệ dựa trên các Data Annotations đã định không.
            if (!ModelState.IsValid)
            {
                // Nếu mô hình không hợp lệ, trả về lỗi Bad Request (HTTP 400) kèm theo chi tiết lỗi xác thực.
                return BadRequest(ModelState);
            }

            // Bước 2: Truy vấn bảng Clients để xác minh ClientId được cung cấp có tồn tại không.
            // Điều này hữu ích trong kiến trúc đa client/đa ứng dụng.
            var client = _context.Clients
                .FirstOrDefault(c => c.ClientId == loginDto.ClientId);

            // Nếu client không tồn tại, trả về phản hồi Unauthorized (HTTP 401).
            if (client == null)
            {
                return Unauthorized("Invalid client credentials.");
            }

            // Bước 3: Truy xuất người dùng từ bảng Users bằng cách khớp email (không phân biệt hoa thường).
            // Đồng thời, eager load (tải trước) các UserRoles và Role liên quan để sử dụng sau này (cho claims).
            var user = await _context.Users
                .Include(u => u.UserRoles) // Bao gồm thuộc tính điều hướng UserRoles (bảng trung gian).
                    .ThenInclude(ur => ur.Role) // Sau đó, bao gồm thuộc tính Role bên trong mỗi UserRole.
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

            // Nếu người dùng không tồn tại, trả về phản hồi Unauthorized (HTTP 401).
            if (user == null)
            {
                // Vì lý do bảo mật, tránh chỉ rõ liệu client hay người dùng không hợp lệ.
                // Chỉ trả về thông báo chung "Invalid credentials."
                return Unauthorized("Invalid credentials.");
            }

            // Bước 4: Xác minh mật khẩu được cung cấp với mật khẩu đã băm (hashed) được lưu trữ, sử dụng BCrypt.
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password);

            // Nếu mật khẩu không hợp lệ, trả về phản hồi Unauthorized (HTTP 401).
            if (!isPasswordValid)
            {
                // Tương tự, tránh chỉ rõ nguyên nhân cụ thể để tăng cường bảo mật.
                return Unauthorized("Invalid credentials.");
            }

            // Bước 5: Tại thời điểm này, xác thực thành công. Tiếp tục tạo một JWT token.
            var token = GenerateJwtToken(user, client);

            // Bước 6: Trả về token đã tạo trong phản hồi OK (HTTP 200).
            return Ok(new { Token = token });
        }

        // ---

        // Phương thức private chịu trách nhiệm tạo một JWT token cho người dùng đã xác thực.
        private string GenerateJwtToken(User user, Client client)
        {
            // Bước 1: Truy xuất khóa ký (signing key) đang hoạt động từ bảng SigningKeys.
            // Đây là nơi dịch vụ KeyRotationService đảm bảo luôn có một khóa hợp lệ.
            var signingKey = _context.SigningKeys.FirstOrDefault(k => k.IsActive);

            // Nếu không tìm thấy khóa ký hoạt động nào, ném ra một ngoại lệ.
            if (signingKey == null)
            {
                throw new Exception("No active signing key available.");
            }

            // Bước 2: Chuyển đổi chuỗi khóa riêng tư (Base64-encoded) trở lại thành mảng byte.
            var privateKeyBytes = Convert.FromBase64String(signingKey.PrivateKey);

            // Bước 3: Tạo một thể hiện RSA mới cho các hoạt động mã hóa.
            var rsa = RSA.Create();

            // Bước 4: Import khóa riêng tư RSA vào thể hiện RSA.
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

            // Bước 5: Tạo một RsaSecurityKey mới sử dụng thể hiện RSA vừa tạo.
            var rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                // Gán Key ID để liên kết JWT với khóa công khai đúng khi xác minh.
                KeyId = signingKey.KeyId
            };

            // Bước 6: Định nghĩa thông tin xác thực ký (SigningCredentials) sử dụng RsaSecurityKey và chỉ định thuật toán ký (RSA-SHA256).
            var creds = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            // Bước 7: Khởi tạo danh sách các claims (tuyên bố) để đưa vào JWT.
            var claims = new List<Claim>
            {
                // Claim 'sub' (subject - chủ thể) với ID của người dùng.
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                // Claim 'jti' (JWT ID - ID JWT) với một định danh duy nhất cho mỗi token (GUID mới).
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Claim 'name' với tên đầu của người dùng.
                new Claim(ClaimTypes.Name, user.Firstname),

                // Claim 'nameidentifier' với email của người dùng (để xác định danh tính).
                new Claim(ClaimTypes.NameIdentifier, user.Email),

                // Claim 'email' với email của người dùng.
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Bước 8: Lặp qua các vai trò của người dùng và thêm mỗi vai trò dưới dạng một Claim loại 'Role'.
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
            }

            // Bước 9: Định nghĩa các thuộc tính của JWT token: issuer, audience, claims, expiration, và signing credentials.
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"], // Issuer của token, thường là URL của ứng dụng của bạn.
                audience: client.ClientURL, // Đối tượng dự định nhận token, thường là URL của ứng dụng client.
                claims: claims, // Danh sách các claims sẽ được đưa vào token.
                expires: DateTime.UtcNow.AddHours(1), // Thời gian hết hạn của token, đặt là 1 giờ kể từ thời điểm hiện tại (UTC).
                signingCredentials: creds // Thông tin xác thực dùng để ký token.
            );

            // Bước 10: Tạo một JWT token handler để tuần tự hóa (serialize) token.
            var tokenHandler = new JwtSecurityTokenHandler();

            // Bước 11: Tuần tự hóa đối tượng token thành một chuỗi (chuỗi JWT thực tế).
            var token = tokenHandler.WriteToken(tokenDescriptor);

            // Bước 12: Trả về chuỗi JWT đã được tuần tự hóa.
            return token;
        }
    }
}
