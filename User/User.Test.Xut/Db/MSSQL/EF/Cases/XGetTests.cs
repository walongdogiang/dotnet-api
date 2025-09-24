using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using User.Svc;

namespace User.Test.Xut.Db.MSSQL.EF.Cases
{
    public sealed class XGetTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IUsersSvc _svc;

        public XGetTests()
        {
            var services = new ServiceCollection()
                .AddSingleton<ITimeProvider, SystemTimeProvider>()

                // Nếu bạn muốn dùng EF: thay bằng EFUsrsSvc và add DbContext InMemory như các test khác
                .AddSingleton<IUsersSvc, UsersSvc>();

            _provider = services.BuildServiceProvider();
            _svc = _provider.GetRequiredService<IUsersSvc>();
        }

        [Theory]
        [InlineData("1", "Nguyen Van A", true)]
        [InlineData("2", "Tran Thi B", false)]
        [InlineData("3", "Le Van C", true)]
        public void GetById_ReturnsExpectedUser(string id, string fullName, bool active)
        {
            var found = _svc.GetById(id);

            Assert.NotNull(found);
            Assert.Equal(id, found!.Id);
            Assert.Equal(fullName, found.FullName);
            Assert.Equal(active, found.Active);
        }

        [Fact]
        public void GetAll_AddTwo_IncreasesCountByTwo()
        {
            var countUsers = _svc.GetAll().Count;
            _svc.Create(new Usr { Id = "10", FullName = "Jully", Active = true });
            _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

            var users = _svc.GetAll();
            Assert.NotNull(users);
            Assert.Equal(countUsers + 2, users.Count);
        }

        public void Dispose() => _provider.Dispose();

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
    }
}