using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using User.Svc;

namespace User.Test.Mst.Cases
{
    [TestClass]
    public sealed class IntegrationPostTests
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        [TestInitialize]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.ConfigureServices(services =>
                    {
                        // Override services for testing if needed
                        services.AddLogging(logging => logging.AddConsole());
                    });
                });

            _client = _factory.CreateClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task TestPostUser_FullIntegration()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "integration001",
                FullName = "Integration Test User",
                Address = "123 Integration Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Full integration test user",
                Active = true
            };

            // Act - POST user
            var postResponse = await _client.PostAsJsonAsync("/usr", newUser);

            // Assert - POST response
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);
            
            var postContent = await postResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"POST Response: {postContent}");

            // Act - GET user by ID
            var getResponse = await _client.GetAsync($"/usr/{newUser.Id}");

            // Assert - GET response
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            
            var retrievedUser = await getResponse.Content.ReadFromJsonAsync<Usr>();
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(newUser.Id, retrievedUser.Id);
            Assert.AreEqual(newUser.FullName, retrievedUser.FullName);
            Assert.AreEqual(newUser.Address, retrievedUser.Address);
            Assert.AreEqual(newUser.Active, retrievedUser.Active);
        }

        [TestMethod]
        public async Task TestPostUser_ThenGetAllUsers()
        {
            // Arrange
            var users = new List<Usr>
            {
                new Usr { Id = "all001", FullName = "User for GetAll 1", Active = true },
                new Usr { Id = "all002", FullName = "User for GetAll 2", Active = false },
                new Usr { Id = "all003", FullName = "User for GetAll 3", Active = true }
            };

            // Act - Create users
            foreach (var user in users)
            {
                var response = await _client.PostAsJsonAsync("/usr", user);
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            }

            // Act - Get all users
            var getAllResponse = await _client.GetAsync("/usrs");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, getAllResponse.StatusCode);
            
            var allUsers = await getAllResponse.Content.ReadFromJsonAsync<List<Usr>>();
            Assert.IsNotNull(allUsers);
            
            // Verify our created users are in the list
            foreach (var user in users)
            {
                var foundUser = allUsers.FirstOrDefault(u => u.Id == user.Id);
                Assert.IsNotNull(foundUser, $"User {user.Id} should be found in GetAll");
                Assert.AreEqual(user.FullName, foundUser.FullName);
            }
        }

        [TestMethod]
        public async Task TestPostUser_ThenUpdateUser()
        {
            // Arrange
            var originalUser = new Usr
            {
                Id = "update001",
                FullName = "Original Name",
                Address = "Original Address",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Original Description",
                Active = true
            };

            // Act - Create user
            var postResponse = await _client.PostAsJsonAsync("/usr", originalUser);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            // Act - Update user
            var updatedUser = new Usr
            {
                Id = originalUser.Id,
                FullName = "Updated Name",
                Address = "Updated Address",
                BirthDay = DateTime.Parse("1995-05-15"),
                Description = "Updated Description",
                Active = false
            };

            var putResponse = await _client.PutAsJsonAsync("/usr", updatedUser);

            // Assert - Update response
            Assert.AreEqual(HttpStatusCode.OK, putResponse.StatusCode);

            // Act - Get updated user
            var getResponse = await _client.GetAsync($"/usr/{originalUser.Id}");

            // Assert - Verify update
            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            
            var retrievedUser = await getResponse.Content.ReadFromJsonAsync<Usr>();
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual(updatedUser.FullName, retrievedUser.FullName);
            Assert.AreEqual(updatedUser.Address, retrievedUser.Address);
            Assert.AreEqual(updatedUser.Active, retrievedUser.Active);
        }

        [TestMethod]
        public async Task TestPostUser_ThenDeleteUser()
        {
            // Arrange
            var userToDelete = new Usr
            {
                Id = "delete001",
                FullName = "User to Delete",
                Address = "Delete Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "This user will be deleted",
                Active = true
            };

            // Act - Create user
            var postResponse = await _client.PostAsJsonAsync("/usr", userToDelete);
            Assert.AreEqual(HttpStatusCode.Created, postResponse.StatusCode);

            // Act - Delete user
            var deleteResponse = await _client.DeleteAsync($"/usr/{userToDelete.Id}");

            // Assert - Delete response
            Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode);

            // Act - Try to get deleted user
            var getResponse = await _client.GetAsync($"/usr/{userToDelete.Id}");

            // Assert - User should not be found
            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [TestMethod]
        public async Task TestPostUser_ErrorScenarios()
        {
            // Test 1: Duplicate ID
            var user1 = new Usr { Id = "error001", FullName = "First User", Active = true };
            var user2 = new Usr { Id = "error001", FullName = "Second User", Active = false };

            var response1 = await _client.PostAsJsonAsync("/usr", user1);
            var response2 = await _client.PostAsJsonAsync("/usr", user2);

            Assert.AreEqual(HttpStatusCode.Created, response1.StatusCode);
            Assert.AreEqual(HttpStatusCode.BadRequest, response2.StatusCode);

            // Test 2: Invalid user data
            var invalidUser = new Usr { Id = "", FullName = "Invalid User", Active = true };
            var invalidResponse = await _client.PostAsJsonAsync("/usr", invalidUser);
            Assert.AreEqual(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

            // Test 3: Get non-existent user
            var notFoundResponse = await _client.GetAsync("/usr/nonexistent");
            Assert.AreEqual(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
        }

        [TestMethod]
        public async Task TestPostUser_WithCustomTestServer()
        {
            // Create a custom test server with specific configuration
            using var customFactory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.ConfigureServices(services =>
                    {
                        // Add any custom services for testing
                    });
                });

            using var customClient = customFactory.CreateClient();

            // Arrange
            var testUser = new Usr
            {
                Id = "custom001",
                FullName = "Custom Test User",
                Address = "Custom Test Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Test with custom server configuration",
                Active = true
            };

            // Act
            var response = await customClient.PostAsJsonAsync("/usr", testUser);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Custom Server Response: {responseContent}");
        }

        [TestMethod]
        public async Task TestPostUser_PerformanceTest()
        {
            var startTime = DateTime.UtcNow;
            var tasks = new List<Task<HttpResponseMessage>>();

            // Create multiple users concurrently
            for (int i = 1; i <= 10; i++)
            {
                var user = new Usr
                {
                    Id = $"perf{i:D3}",
                    FullName = $"Performance User {i}",
                    Address = $"Performance Street {i}",
                    BirthDay = DateTime.Parse("1990-01-01"),
                    Description = $"Performance test user {i}",
                    Active = true
                };

                tasks.Add(_client.PostAsJsonAsync("/usr", user));
            }

            // Wait for all requests to complete
            var responses = await Task.WhenAll(tasks);
            var endTime = DateTime.UtcNow;

            // Assert all requests succeeded
            foreach (var response in responses)
            {
                Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            }

            var duration = endTime - startTime;
            Console.WriteLine($"Created 10 users in {duration.TotalMilliseconds}ms");
            
            // Performance assertion (adjust threshold as needed)
            Assert.IsTrue(duration.TotalSeconds < 10, "Performance test should complete within 10 seconds");
        }
    }
}
