using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using User.Svc;

namespace User.Test.Nun.Db.MSSQL.EF.Cases
{
    [TestFixture]
    public sealed class NGetTests
    {
        private IUsersSvc _svc = null!;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection()
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IUsersSvc, UsersSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<IUsersSvc>();
        }

        [TestCase("1", "Nguyen Van A", true)]
        [TestCase("2", "Tran Thi B", false)]
        [TestCase("3", "Le Van C", true)]
        public void GetById_ReturnsExpectedUser(string id, string fullName, bool active)
        {
            TestContext.Out.WriteLine($"GetById executed at: {DateTime.Now}");

            var found = _svc.GetById(id);

            Assert.That(found, Is.Not.Null, $"User with ID '{id}' should exist.");
            Assert.That(found!.Id, Is.EqualTo(id), $"Id should match with '{id}'.");
            Assert.That(found.FullName, Is.EqualTo(fullName), $"Fullname should match with '{fullName}'.");
            Assert.That(found.Active, Is.EqualTo(active), $"Active status should match with '{active}'.");
        }

        [Test]
        public void GetAll_AddTwo_IncreasesCountByTwo()
        {
            var countUsers = _svc.GetAll().Count;
            _svc.Create(new Usr { Id = "10", FullName = "Jully", Active = true });
            _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

            var users = _svc.GetAll();
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.EqualTo(countUsers + 2), "User count should increase by 2 after adding 2 users.");
        }

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}