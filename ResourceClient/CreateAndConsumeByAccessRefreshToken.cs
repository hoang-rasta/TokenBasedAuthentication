using ResourceClient.DTOs;
using ResourceClient.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ResourceClient
{
    public static class CreateAndConsumeByAccessRefreshToken
    {
        // Cài đặt cấu hình
        private static readonly string AuthServerBaseUrl = "https://localhost:7296"; // URL của Authentication Server
        private static readonly string ResourceServerBaseUrl = "https://localhost:7289"; // Thay thế bằng URL và cổng của Resource Server
        private static readonly string ClientId = "Client1"; // Phải trùng với một ClientId hợp lệ trong Authentication Server
        private static readonly string UserEmail = "hoanganh@example.com"; // Thay thế bằng email của người dùng đã đăng ký
        private static readonly string UserPassword = "Password@123;"; // Thay thế bằng mật khẩu của người dùng đã đăng ký

        // Token storage instance
        private static readonly TokenStorage tokenStorage = new TokenStorage();
        // HttpClient instance (shared)
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task Run()
        {
            try
            {
                // Step 1: Authenticate and obtain JWT token and Refresh Token
                Console.OutputEncoding = Encoding.UTF8; // Ensure console supports Vietnamese characters
                await Task.Delay(5000); // Simulate delay for demonstration purposes
                var loginSuccess = await AuthenticateAsync(UserEmail, UserPassword, ClientId);
                if (!loginSuccess)
                {
                    Console.WriteLine("Authentication failed. Exiting...");
                    return;
                }
                Console.WriteLine("Authentication successful. Tokens obtained.\n");
                // Step 2: Consume Resource Server's ProductsController endpoints
                await ConsumeResourceServerAsync();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                httpClient.Dispose();
            }
        }
        // Authenticates the user with the Authentication Server and retrieves JWT and Refresh tokens.
        private static async Task<bool> AuthenticateAsync(string email, string password, string clientId)
        {
            var loginUrl = $"{AuthServerBaseUrl}/api/Auth/Login";
            var loginData = new
            {
                Email = email,
                Password = password,
                ClientId = clientId
            };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            Console.WriteLine("Sending authentication request...");
            var response = await httpClient.PostAsync(loginUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Authentication failed with status code: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
                return false;
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDTO>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token) && !string.IsNullOrEmpty(loginResponse.RefreshToken))
            {
                tokenStorage.AccessToken = loginResponse.Token;
                tokenStorage.RefreshToken = loginResponse.RefreshToken;
                tokenStorage.ClientId = clientId;
                return true;
            }
            Console.WriteLine("Token not found in the authentication response.\n");
            return false;
        }
        // Consumes the Resource Server's ProductsController endpoints using the JWT token.
        private static async Task ConsumeResourceServerAsync()
        {
            // Set the Authorization header with the Bearer token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStorage.AccessToken);
            // Create a new product
            var newProduct = new
            {
                Name = "Smartphone",
                Description = "A high-end smartphone with excellent features.",
                Price = 999.99
            };
            Console.WriteLine("Creating a new product...");
            var createResponse = await httpClient.PostAsync(
            $"{ResourceServerBaseUrl}/api/Products/Add",
            new StringContent(JsonSerializer.Serialize(newProduct), Encoding.UTF8, "application/json"));
            if (createResponse.IsSuccessStatusCode)
            {
                var createdProductJson = await createResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Product created successfully: {createdProductJson}\n");
            }
            else if (createResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access token expired or invalid. Attempting to refresh token...");
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    // Retry the request with the new token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStorage.AccessToken);
                    createResponse = await httpClient.PostAsync(
                    $"{ResourceServerBaseUrl}/api/Products/Add",
                    new StringContent(JsonSerializer.Serialize(newProduct), Encoding.UTF8, "application/json"));
                    if (createResponse.IsSuccessStatusCode)
                    {
                        var createdProductJson = await createResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Product created successfully after token refresh: {createdProductJson}\n");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create product after token refresh. Status Code: {createResponse.StatusCode}");
                        var errorContent = await createResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {errorContent}\n");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to refresh token. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Failed to create product. Status Code: {createResponse.StatusCode}");
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
            }
            // Step Retrieve all products
            Console.WriteLine("Retrieving all products...");
            var getAllResponse = await httpClient.GetAsync($"{ResourceServerBaseUrl}/api/Products/GetAll");
            if (getAllResponse.IsSuccessStatusCode)
            {
                var productsJson = await getAllResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Products: {productsJson}\n");
            }
            else if (getAllResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access token expired or invalid. Attempting to refresh token...");
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    // Retry the request with the new token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStorage.AccessToken);
                    getAllResponse = await httpClient.GetAsync($"{ResourceServerBaseUrl}/api/Products/GetAll");
                    if (getAllResponse.IsSuccessStatusCode)
                    {
                        var productsJson = await getAllResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Products after token refresh: {productsJson}\n");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve products after token refresh. Status Code: {getAllResponse.StatusCode}");
                        var errorContent = await getAllResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {errorContent}\n");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to refresh token. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve products. Status Code: {getAllResponse.StatusCode}");
                var errorContent = await getAllResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
            }
            // Step Retrieve a specific product by ID
            Console.WriteLine("Retrieving product with ID 1...");
            var getByIdResponse = await httpClient.GetAsync($"{ResourceServerBaseUrl}/api/Products/GetById/1");
            if (getByIdResponse.IsSuccessStatusCode)
            {
                var productJson = await getByIdResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Product Details: {productJson}\n");
            }
            else if (getByIdResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access token expired or invalid. Attempting to refresh token...");
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    // Retry the request with the new token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStorage.AccessToken);
                    getByIdResponse = await httpClient.GetAsync($"{ResourceServerBaseUrl}/api/Products/GetById/1");
                    if (getByIdResponse.IsSuccessStatusCode)
                    {
                        var productJson = await getByIdResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Product Details after token refresh: {productJson}\n");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve product after token refresh. Status Code: {getByIdResponse.StatusCode}");
                        var errorContent = await getByIdResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {errorContent}\n");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to refresh token. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve product. Status Code: {getByIdResponse.StatusCode}");
                var errorContent = await getByIdResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
            }
            // Step Update a product
            var updatedProduct = new
            {
                Name = "Smartphone Pro",
                Description = "An upgraded smartphone with enhanced features.",
                Price = 1199.99
            };
            Console.WriteLine("Updating product with ID 1...");
            var updateResponse = await httpClient.PutAsync(
            $"{ResourceServerBaseUrl}/api/Products/Update/1",
            new StringContent(JsonSerializer.Serialize(updatedProduct), Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode || updateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine("Product updated successfully.\n");
            }
            else if (updateResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access token expired or invalid. Attempting to refresh token...");
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    // Retry the request with the new token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStorage.AccessToken);
                    updateResponse = await httpClient.PutAsync(
                    $"{ResourceServerBaseUrl}/api/Products/Update/1",
                    new StringContent(JsonSerializer.Serialize(updatedProduct), Encoding.UTF8, "application/json"));
                    if (updateResponse.IsSuccessStatusCode || updateResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        Console.WriteLine("Product updated successfully after token refresh.\n");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to update product after token refresh. Status Code: {updateResponse.StatusCode}");
                        var errorContent = await updateResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {errorContent}\n");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to refresh token. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Failed to update product. Status Code: {updateResponse.StatusCode}");
                var errorContent = await updateResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
            }
            // Step Delete a product
            Console.WriteLine("Deleting product with ID 1...");
            var deleteResponse = await httpClient.DeleteAsync($"{ResourceServerBaseUrl}/api/Products/Delete/1");
            if (deleteResponse.IsSuccessStatusCode || deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine("Product deleted successfully.\n");
            }
            else if (deleteResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Access token expired or invalid. Attempting to refresh token...");
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    // Retry the request with the new token
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStorage.AccessToken);
                    deleteResponse = await httpClient.DeleteAsync($"{ResourceServerBaseUrl}/api/Products/Delete/1");
                    if (deleteResponse.IsSuccessStatusCode || deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        Console.WriteLine("Product deleted successfully after token refresh.\n");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to delete product after token refresh. Status Code: {deleteResponse.StatusCode}");
                        var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error: {errorContent}\n");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to refresh token. Exiting...");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Failed to delete product. Status Code: {deleteResponse.StatusCode}");
                var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
            }
        }
        // Refreshes the access token using the refresh token
        private static async Task<bool> RefreshTokenAsync()
        {
            var refreshUrl = $"{AuthServerBaseUrl}/api/Auth/RefreshToken";
            var refreshData = new RefreshTokenRequestDTO
            {
                RefreshToken = tokenStorage.RefreshToken,
                ClientId = tokenStorage.ClientId
            };
            var content = new StringContent(JsonSerializer.Serialize(refreshData), Encoding.UTF8, "application/json");
            Console.WriteLine("Attempting to refresh access token...");
            var response = await httpClient.PostAsync(refreshUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Token refresh failed with status code: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}\n");
                return false;
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            var refreshResponse = JsonSerializer.Deserialize<RefreshTokenResponseDTO>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.Token) && !string.IsNullOrEmpty(refreshResponse.RefreshToken))
            {
                tokenStorage.AccessToken = refreshResponse.Token;
                tokenStorage.RefreshToken = refreshResponse.RefreshToken;
                Console.WriteLine("Access token refreshed successfully.\n");
                return true;
            }
            Console.WriteLine("Failed to parse refresh token response.\n");
            return false;
        }
    }
}
