using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WebApi.JWTAuthServer.Models;

namespace WebApi.JWTAuthServer.Services
{
    // Lớp này định nghĩa một Background Service định kỳ xoay vòng các khóa mã hóa.
    public class KeyRotationService : BackgroundService
    {
        // IServiceProvider được sử dụng để tạo một phạm vi dịch vụ (scoped service lifetime).
        private readonly IServiceProvider _serviceProvider;

        // Đặt tần suất xoay vòng khóa; ở đây là mỗi 7 ngày.
        private readonly TimeSpan _rotationInterval = TimeSpan.FromDays(7);

        // Hàm tạo (constructor) chấp nhận một IServiceProvider để tiêm phụ thuộc (dependency injection).
        public KeyRotationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Phương thức này được thực thi khi dịch vụ nền khởi động.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Vòng lặp chạy cho đến khi dịch vụ bị dừng.
            while (!stoppingToken.IsCancellationRequested)
            {
                // Thực hiện logic xoay vòng khóa.
                await RotateKeysAsync();

                // Đợi khoảng thời gian xoay vòng đã cấu hình trước khi chạy lại.
                await Task.Delay(_rotationInterval, stoppingToken);
            }
        }

        // Phương thức này xử lý logic xoay vòng khóa thực tế.
        private async Task RotateKeysAsync()
        {
            // Tạo một phạm vi dịch vụ mới cho việc tiêm phụ thuộc.
            using var scope = _serviceProvider.CreateScope();

            // Lấy ngữ cảnh cơ sở dữ liệu (ApplicationDbContext) từ IServiceProvider.
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Truy vấn cơ sở dữ liệu để lấy khóa ký hiện đang hoạt động.
            var activeKey = await context.SigningKeys.FirstOrDefaultAsync(k => k.IsActive);

            // Kiểm tra xem có khóa hoạt động nào không hoặc nếu khóa hoạt động sắp hết hạn.
            if (activeKey == null || activeKey.ExpiresAt <= DateTime.UtcNow.AddDays(10))
            {
                // Nếu có một khóa hoạt động, hãy đánh dấu nó là không hoạt động.
                if (activeKey != null)
                {
                    // Đánh dấu khóa hiện tại là không hoạt động vì nó sắp được thay thế.
                    activeKey.IsActive = false;
                    // Cập nhật khóa hiện tại trong cơ sở dữ liệu.
                    context.SigningKeys.Update(activeKey);
                }

                // Tạo một cặp khóa RSA mới.
                using var rsa = RSA.Create(2048);

                // Xuất khóa riêng tư dưới dạng chuỗi được mã hóa Base64.
                var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

                // Xuất khóa công khai dưới dạng chuỗi được mã hóa Base64.
                var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

                // Tạo một mã định danh duy nhất cho khóa mới.
                var newKeyId = Guid.NewGuid().ToString();

                // Tạo một thực thể SigningKey mới với các chi tiết khóa RSA mới.
                var newKey = new SigningKey
                {
                    KeyId = newKeyId,
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1) // Đặt khóa mới hết hạn sau một năm.
                };

                // Thêm khóa mới vào cơ sở dữ liệu.
                await context.SigningKeys.AddAsync(newKey);

                // Lưu các thay đổi vào cơ sở dữ liệu.
                await context.SaveChangesAsync();
            }
        }
    }
}
