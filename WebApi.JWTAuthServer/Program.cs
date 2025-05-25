
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApi.JWTAuthServer.Services;

namespace WebApi.JWTAuthServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Thêm dịch vụ controller vào container và cấu hình tùy chọn JSON serialization.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Giữ nguyên tên thuộc tính như được định nghĩa trong các mô hình C# (tắt quy ước camelCase).
                    // Điều này có nghĩa là các thuộc tính JSON sẽ khớp với tên thuộc tính C# (ví dụ: "Firstname" thay vì "firstname").
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            // Cấu hình Entity Framework Core sử dụng SQL Server với chuỗi kết nối từ cấu hình.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("EFCoreDBConnection")));

            // Đăng ký KeyRotationService như một dịch vụ được host (Background Service).
            // Dịch vụ này xử lý việc luân chuyển định kỳ các khóa ký để tăng cường bảo mật.
            builder.Services.AddHostedService<KeyRotationService>();

            // Cấu hình Xác thực sử dụng JWT Bearer tokens.
            builder.Services.AddAuthentication(options =>
            {
                // Đây là scheme xác thực mặc định sẽ được sử dụng khi ứng dụng cố gắng xác thực người dùng.
                // Quyết định handler xác thực nào sẽ được dùng để xác minh danh tính người dùng theo mặc định.
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                // Đây là scheme xác thực mặc định sẽ được sử dụng khi ứng dụng gặp phải một yêu cầu xác thực.
                // Quyết định handler xác thực nào sẽ được dùng để phản hồi các yêu cầu xác thực hoặc ủy quyền thất bại.
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Thêm xác thực JWT Bearer cụ thể.
            .AddJwtBearer(options =>
            {
                // Định nghĩa các tham số xác thực token để đảm bảo token hợp lệ và đáng tin cậy.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // Đảm bảo token được cấp bởi một Issuer đáng tin cậy.
                    ValidIssuer = builder.Configuration["Jwt:Issuer"], // Giá trị Issuer dự kiến từ cấu hình.
                    ValidateAudience = false, // Tắt xác thực Audience (có thể bật khi cần).
                    ValidateLifetime = true, // Đảm bảo token chưa hết hạn.
                    ValidateIssuerSigningKey = true, // Đảm bảo khóa ký của token hợp lệ.

                    // Định nghĩa một IssuerSigningKeyResolver tùy chỉnh để động lấy các khóa ký từ endpoint JWKS.
                    // Resolver này được gọi bởi middleware JWT Bearer khi nó cần tìm khóa công khai đúng để xác thực chữ ký của token.
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        // (Tùy chọn) Các dòng để debug:
                        //Console.WriteLine(<span class="math-inline">"Received Token\: \{token\}"\);
                        //Console\.WriteLine\(</span>"Token Issuer: {securityToken.Issuer}");
                        //Console.WriteLine(<span class="math-inline">"Key ID\: \{kid\}"\); // Đây là 'kid' từ header của JWT\.
                        //Console\.WriteLine\(</span>"Validate Lifetime: {parameters.ValidateLifetime}");

                        // Khởi tạo một thể hiện HttpClient để lấy JWKS.
                        var httpClient = new HttpClient();
                        // Lấy JWKS (JSON Web Key Set) một cách đồng bộ từ URL được chỉ định.
                        // URL được xây dựng bằng cách sử dụng Issuer JWT đã cấu hình và đường dẫn JWKS tiêu chuẩn.
                        var jwks = httpClient.GetStringAsync($"{builder.Configuration["Jwt:Issuer"]}/.well-known/jwks.json").Result;
                        // Phân tích chuỗi JWKS đã lấy được thành một đối tượng JsonWebKeySet.
                        var keys = new JsonWebKeySet(jwks);
                        // Trả về tập hợp các đối tượng JsonWebKey. Middleware JWT Bearer sau đó sẽ
                        // sử dụng các khóa này để tìm khóa khớp với 'kid' trong JWT đến và xác minh chữ ký.
                        return keys.Keys;
                    }
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
