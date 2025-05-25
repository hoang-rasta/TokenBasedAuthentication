using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ResourceClient
{
    public static class CreateAndConsumeByAccessToken
    {
        // Cấu hình các thông số
        private static readonly string AuthServerBaseUrl = "https://localhost:7296"; // URL của Authentication Server
        private static readonly string ResourceServerBaseUrl = "https://localhost:7289"; // Thay thế bằng URL và cổng của Resource Server
        private static readonly string ClientId = "Client1"; // Phải trùng với một ClientId hợp lệ trong Authentication Server
        private static readonly string UserEmail = "hoanganh@example.com"; // Thay thế bằng email của người dùng đã đăng ký
        private static readonly string UserPassword = "Password@123;"; // Thay thế bằng mật khẩu của người dùng đã đăng ký


        public static async Task Run()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8; // Đảm bảo console hỗ trợ tiếng Việt
                await Task.Delay(5000);
                // Bước 1: Xác thực và lấy JWT token
                var token = await AuthenticateAsync(UserEmail, UserPassword, ClientId);
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Xác thực thất bại. Kết thúc...");
                    return;
                }
                Console.WriteLine("Xác thực thành công. Đã lấy JWT Token.\n");

                // Bước 2: Tiêu thụ các Endpoint của Resource Server
                await ConsumeResourceServerAsync(token);

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Đã xảy ra lỗi: {ex.Message}");
            }
        }

        // Xác thực người dùng với Authentication Server và lấy JWT token
        private static async Task<string?> AuthenticateAsync(string email, string password, string clientId)
        {
            using var httpClient = new HttpClient();
            var loginUrl = $"{AuthServerBaseUrl}/api/Auth/Login";
            var loginData = new
            {
                Email = email,
                Password = password,
                ClientId = clientId
            };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            Console.WriteLine("Đang gửi yêu cầu xác thực...");
            var response = await httpClient.PostAsync(loginUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Xác thực thất bại với mã trạng thái: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi: {errorContent}\n");
                return null;
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);
            if (jsonDoc.RootElement.TryGetProperty("Token", out var tokenElement))
            {
                return tokenElement.GetString();
            }
            Console.WriteLine("Không tìm thấy Token trong phản hồi xác thực.\n");
            return null;
        }

        // Tiêu thụ các Endpoint của Resource Server sử dụng JWT token
        private static async Task ConsumeResourceServerAsync(string token)
        {
            using var httpClient = new HttpClient();
            // Thiết lập Authorization header với Bearer token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Tạo một sản phẩm mới
            var newProduct = new
            {
                Name = "Smartphone",
                Description = "Điện thoại thông minh cao cấp với tính năng xuất sắc.",
                Price = 999.99
            };
            Console.WriteLine("Đang tạo sản phẩm mới...");
            var createResponse = await httpClient.PostAsync(
                $"{ResourceServerBaseUrl}/api/Products/Add",
                new StringContent(JsonSerializer.Serialize(newProduct), Encoding.UTF8, "application/json"));
            if (createResponse.IsSuccessStatusCode)
            {
                var createdProductJson = await createResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Sản phẩm đã được tạo thành công: {createdProductJson}\n");
            }
            else
            {
                Console.WriteLine($"Tạo sản phẩm thất bại. Mã trạng thái: {createResponse.StatusCode}");
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi: {errorContent}\n");
            }

            // Bước 3: Lấy tất cả sản phẩm
            Console.WriteLine("Đang lấy tất cả sản phẩm...");
            var getAllResponse = await httpClient.GetAsync($"{ResourceServerBaseUrl}/api/Products/GetAll");
            if (getAllResponse.IsSuccessStatusCode)
            {
                var productsJson = await getAllResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Danh sách sản phẩm: {productsJson}\n");
            }
            else
            {
                Console.WriteLine($"Lấy sản phẩm thất bại. Mã trạng thái: {getAllResponse.StatusCode}");
                var errorContent = await getAllResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi: {errorContent}\n");
            }

            // Bước 4: Lấy một sản phẩm cụ thể theo ID
            Console.WriteLine("Đang lấy sản phẩm với ID 1...");
            var getByIdResponse = await httpClient.GetAsync($"{ResourceServerBaseUrl}/api/Products/GetById/1");
            if (getByIdResponse.IsSuccessStatusCode)
            {
                var productJson = await getByIdResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Chi tiết sản phẩm: {productJson}\n");
            }
            else
            {
                Console.WriteLine($"Lấy sản phẩm thất bại. Mã trạng thái: {getByIdResponse.StatusCode}");
                var errorContent = await getByIdResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi: {errorContent}\n");
            }

            // Bước 5: Cập nhật một sản phẩm
            var updatedProduct = new
            {
                Name = "Smartphone Pro",
                Description = "Điện thoại thông minh nâng cấp với tính năng cải tiến.",
                Price = 1199.99
            };
            Console.WriteLine("Đang cập nhật sản phẩm với ID 1...");
            var updateResponse = await httpClient.PutAsync(
                $"{ResourceServerBaseUrl}/api/Products/Update/1",
                new StringContent(JsonSerializer.Serialize(updatedProduct), Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode || updateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine("Sản phẩm đã được cập nhật thành công.\n");
            }
            else
            {
                Console.WriteLine($"Cập nhật sản phẩm thất bại. Mã trạng thái: {updateResponse.StatusCode}");
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi: {errorContent}\n");
            }

            // Bước 6: Xóa một sản phẩm
            Console.WriteLine("Đang xóa sản phẩm với ID 1...");
            var deleteResponse = await httpClient.DeleteAsync($"{ResourceServerBaseUrl}/api/Products/Delete/1");
            if (deleteResponse.IsSuccessStatusCode || deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine("Sản phẩm đã được xóa thành công.\n");
            }
            else
            {
                Console.WriteLine($"Xóa sản phẩm thất bại. Mã trạng thái: {deleteResponse.StatusCode}");
                var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi: {errorContent}\n");
            }
        }

    }
}
