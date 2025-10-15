using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using User.Svc;

namespace User.Test.Mst.Cases
{
    [TestClass]
    public sealed class HttpClientPostTests
    {
        private HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7000"; // Thay đổi port theo cấu hình của bạn

        [TestInitialize]
        public void Setup()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            
            // Bỏ qua SSL certificate validation cho test (chỉ dùng trong development)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "UnitTest");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }

        [TestMethod]
        public async Task TestPostUser_SimpleHttpClient()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "http001",
                FullName = "HttpClient Test User",
                Address = "123 HttpClient Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Test user via HttpClient",
                Active = true
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/usr", newUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response: {responseContent}");
        }

        [TestMethod]
        public async Task TestPostUser_ManualJsonSerialization()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "json001",
                FullName = "JSON Test User",
                Address = "456 JSON Avenue",
                BirthDay = DateTime.Parse("1985-05-15"),
                Description = "Test with manual JSON serialization",
                Active = true
            };

            var json = JsonSerializer.Serialize(newUser, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/usr", content);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"JSON Response: {responseContent}");
        }

        [TestMethod]
        public async Task TestPostUser_WithCustomHeaders()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "header001",
                FullName = "Header Test User",
                Address = "789 Header Street",
                BirthDay = DateTime.Parse("1992-12-25"),
                Description = "Test with custom headers",
                Active = true
            };

            // Add custom headers
            _httpClient.DefaultRequestHeaders.Add("X-Test-Source", "UnitTest");
            _httpClient.DefaultRequestHeaders.Add("X-Test-Version", "1.0");

            // Act
            var response = await _httpClient.PostAsJsonAsync("/usr", newUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            // Verify response headers
            var locationHeader = response.Headers.Location;
            Assert.IsNotNull(locationHeader);
            Console.WriteLine($"Location: {locationHeader}");
        }

        [TestMethod]
        public async Task TestPostUser_ErrorHandling()
        {
            // Arrange
            var invalidUser = new Usr
            {
                Id = "", // Empty ID should cause error
                FullName = "Invalid User",
                Active = true
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/usr", invalidUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Response: {errorContent}");
            Assert.IsTrue(errorContent.Length > 0, "Error response should not be empty");
        }

        [TestMethod]
        public async Task TestPostUser_TimeoutHandling()
        {
            // Arrange
            _httpClient.Timeout = TimeSpan.FromSeconds(5); // Set short timeout
            
            var newUser = new Usr
            {
                Id = "timeout001",
                FullName = "Timeout Test User",
                Address = "Timeout Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Test timeout handling",
                Active = true
            };

            // Act & Assert
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/usr", newUser);
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                Assert.Fail("Request timed out");
            }
        }

        [TestMethod]
        public async Task TestPostUser_VerifyResponseContent()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "verify001",
                FullName = "Verify Response User",
                Address = "Verify Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Verify response content",
                Active = true
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/usr", newUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify response contains expected fields
            Assert.IsTrue(responseObject.TryGetProperty("fullName", out var fullNameProp));
            Assert.AreEqual(newUser.FullName, fullNameProp.GetString());
            
            Assert.IsTrue(responseObject.TryGetProperty("active", out var activeProp));
            Assert.AreEqual(newUser.Active, activeProp.GetBoolean());
        }

        [TestMethod]
        public async Task TestPostUser_ConcurrentRequests()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var userIds = new List<string>();

            for (int i = 1; i <= 5; i++)
            {
                var userId = $"concurrent{i:D3}";
                userIds.Add(userId);
                
                var user = new Usr
                {
                    Id = userId,
                    FullName = $"Concurrent User {i}",
                    Address = $"Concurrent Street {i}",
                    BirthDay = DateTime.Parse("1990-01-01"),
                    Description = $"Concurrent test user {i}",
                    Active = true
                };

                tasks.Add(_httpClient.PostAsJsonAsync("/usr", user));
            }

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            }

            Console.WriteLine($"Successfully created {responses.Length} users concurrently");
        }
    }
}
