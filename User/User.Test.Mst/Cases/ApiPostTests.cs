using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using User.Svc;

namespace User.Test.Mst.Cases
{
    [TestClass]
    public sealed class ApiPostTests
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        [TestInitialize]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task TestPostUser_Success()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "test001",
                FullName = "Nguyen Van Test",
                Address = "123 Test Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Test user for API",
                Active = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/usr", newUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(responseContent.Contains("Nguyen Van Test"));
            Assert.IsTrue(responseContent.Contains("true"));
        }

        [TestMethod]
        public async Task TestPostUser_DuplicateId_ShouldReturnBadRequest()
        {
            // Arrange
            var user1 = new Usr
            {
                Id = "dup001",
                FullName = "First User",
                Active = true
            };

            var user2 = new Usr
            {
                Id = "dup001", // Same ID
                FullName = "Second User",
                Active = false
            };

            // Act
            var response1 = await _client.PostAsJsonAsync("/usr", user1);
            var response2 = await _client.PostAsJsonAsync("/usr", user2);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response1.StatusCode);
            Assert.AreEqual(HttpStatusCode.BadRequest, response2.StatusCode);
            
            var errorMessage = await response2.Content.ReadAsStringAsync();
            Assert.IsTrue(errorMessage.Contains("already exists") || errorMessage.Contains("duplicate"));
        }

        [TestMethod]
        public async Task TestPostUser_InvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidUser = new Usr
            {
                Id = "", // Empty ID should be invalid
                FullName = "Test User",
                Active = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/usr", invalidUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task TestPostUser_WithHttpClient_ManualJson()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "manual001",
                FullName = "Manual Test User",
                Address = "456 Manual Street",
                BirthDay = DateTime.Parse("1985-05-15"),
                Description = "Manual JSON test",
                Active = true
            };

            var json = JsonSerializer.Serialize(newUser);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/usr", content);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            // Verify the user was created by getting it
            var getResponse = await _client.GetAsync($"/usr/{newUser.Id}");
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            
            var retrievedUser = await getResponse.Content.ReadFromJsonAsync<Usr>();
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(newUser.Id, retrievedUser.Id);
            Assert.AreEqual(newUser.FullName, retrievedUser.FullName);
        }

        [TestMethod]
        public async Task TestPostUser_VerifyCreatedUser()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "verify001",
                FullName = "Verify Test User",
                Address = "789 Verify Avenue",
                BirthDay = DateTime.Parse("1992-12-25"),
                Description = "User to verify creation",
                Active = true
            };

            // Act
            var postResponse = await _client.PostAsJsonAsync("/usr", newUser);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            // Verify by getting all users
            var getAllResponse = await _client.GetAsync("/usrs");
            Assert.AreEqual(HttpStatusCode.OK, getAllResponse.StatusCode);
            
            var allUsers = await getAllResponse.Content.ReadFromJsonAsync<List<Usr>>();
            Assert.IsNotNull(allUsers);
            
            var createdUser = allUsers.FirstOrDefault(u => u.Id == newUser.Id);
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(newUser.FullName, createdUser.FullName);
            Assert.AreEqual(newUser.Address, createdUser.Address);
            Assert.AreEqual(newUser.Active, createdUser.Active);
        }
    }
}
