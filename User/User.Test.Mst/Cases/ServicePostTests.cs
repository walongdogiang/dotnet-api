using Microsoft.Extensions.DependencyInjection;
using User.Svc;

namespace User.Test.Mst.Cases
{
    [TestClass]
    public sealed class ServicePostTests
    {
        private IUsersSvc _svc;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection()
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IUsersSvc, UsersSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<IUsersSvc>();
        }

        [TestMethod]
        public void TestCreateUser_Success()
        {
            // Arrange
            var newUser = new Usr
            {
                Id = "svc001",
                FullName = "Service Test User",
                Address = "123 Service Street",
                BirthDay = DateTime.Parse("1990-01-01"),
                Description = "Test user for service",
                Active = true
            };

            var countBefore = _svc.GetAll().Count;

            // Act
            var result = _svc.Create(newUser);

            // Assert
            Assert.IsNull(result, "Create should return null on success");
            
            var countAfter = _svc.GetAll().Count;
            Assert.AreEqual(countBefore + 1, countAfter, "User count should increase by 1");

            var createdUser = _svc.GetById(newUser.Id);
            Assert.IsNotNull(createdUser, "Created user should be found");
            Assert.AreEqual(newUser.Id, createdUser.Id);
            Assert.AreEqual(newUser.FullName, createdUser.FullName);
            Assert.AreEqual(newUser.Address, createdUser.Address);
            Assert.AreEqual(newUser.Active, createdUser.Active);
        }

        [TestMethod]
        public void TestCreateUser_DuplicateId_ShouldReturnError()
        {
            // Arrange
            var user1 = new Usr
            {
                Id = "dup002",
                FullName = "First User",
                Active = true
            };

            var user2 = new Usr
            {
                Id = "dup002", // Same ID
                FullName = "Second User",
                Active = false
            };

            // Act
            var result1 = _svc.Create(user1);
            var countAfterFirst = _svc.GetAll().Count;
            var result2 = _svc.Create(user2);
            var countAfterSecond = _svc.GetAll().Count;

            // Assert
            Assert.IsNull(result1, "First create should succeed");
            Assert.IsNotNull(result2, "Second create with duplicate ID should return error");
            Assert.AreEqual(countAfterFirst, countAfterSecond, "Count should not change after duplicate create");
        }

        [TestMethod]
        public void TestCreateUser_InvalidData_ShouldReturnError()
        {
            // Arrange
            var invalidUser = new Usr
            {
                Id = "", // Empty ID should be invalid
                FullName = "Invalid User",
                Active = true
            };

            // Act
            var result = _svc.Create(invalidUser);

            // Assert
            Assert.IsNotNull(result, "Create with invalid data should return error message");
            Assert.IsTrue(result.Contains("Id") || result.Contains("required"), "Error message should mention ID requirement");
        }

        [TestMethod]
        public void TestCreateUser_NullUser_ShouldReturnError()
        {
            // Act
            var result = _svc.Create(null);

            // Assert
            Assert.IsNotNull(result, "Create with null user should return error");
        }

        [TestMethod]
        public void TestCreateUser_WithAllFields()
        {
            // Arrange
            var completeUser = new Usr
            {
                Id = "complete001",
                FullName = "Complete Test User",
                Address = "456 Complete Avenue",
                BirthDay = DateTime.Parse("1985-05-15"),
                Description = "This is a complete test user with all fields filled",
                Active = true
            };

            // Act
            var result = _svc.Create(completeUser);

            // Assert
            Assert.IsNull(result, "Create should succeed with all fields");

            var createdUser = _svc.GetById(completeUser.Id);
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(completeUser.Id, createdUser.Id);
            Assert.AreEqual(completeUser.FullName, createdUser.FullName);
            Assert.AreEqual(completeUser.Address, createdUser.Address);
            Assert.AreEqual(completeUser.BirthDay, createdUser.BirthDay);
            Assert.AreEqual(completeUser.Description, createdUser.Description);
            Assert.AreEqual(completeUser.Active, createdUser.Active);
        }

        [TestMethod]
        public void TestCreateUser_InactiveUser()
        {
            // Arrange
            var inactiveUser = new Usr
            {
                Id = "inactive001",
                FullName = "Inactive Test User",
                Address = "789 Inactive Street",
                BirthDay = DateTime.Parse("1992-12-25"),
                Description = "This user is inactive",
                Active = false
            };

            // Act
            var result = _svc.Create(inactiveUser);

            // Assert
            Assert.IsNull(result, "Create should succeed even for inactive user");

            var createdUser = _svc.GetById(inactiveUser.Id);
            Assert.IsNotNull(createdUser);
            Assert.AreEqual(false, createdUser.Active);
        }

        [TestMethod]
        public void TestCreateUser_MultipleUsers()
        {
            // Arrange
            var users = new List<Usr>
            {
                new Usr { Id = "multi001", FullName = "User 1", Active = true },
                new Usr { Id = "multi002", FullName = "User 2", Active = false },
                new Usr { Id = "multi003", FullName = "User 3", Active = true }
            };

            var countBefore = _svc.GetAll().Count;

            // Act
            foreach (var user in users)
            {
                var result = _svc.Create(user);
                Assert.IsNull(result, $"Create should succeed for user {user.Id}");
            }

            // Assert
            var countAfter = _svc.GetAll().Count;
            Assert.AreEqual(countBefore + users.Count, countAfter, "All users should be created");

            foreach (var user in users)
            {
                var createdUser = _svc.GetById(user.Id);
                Assert.IsNotNull(createdUser, $"User {user.Id} should be found");
                Assert.AreEqual(user.FullName, createdUser.FullName);
            }
        }
    }
}
