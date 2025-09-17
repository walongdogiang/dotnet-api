using Xunit;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Xun.Cases;

public sealed class XGetTests
{
    private readonly IUsersSvc _svc;

    public XGetTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITimeProvider, SystemTimeProvider>()
            .AddSingleton<IUsersSvc, UsersSvc>();

        _svc = services.BuildServiceProvider().GetRequiredService<IUsersSvc>();
    }

    [Theory]
    [InlineData("1", "Nguyen Van A", true)]
    [InlineData("2", "Tran Thi B", false)]
    [InlineData("3", "Le Van C", true)]
    public void GetById_ReturnsExpected(string id, string fullName, bool active)
    {
        var found = _svc.GetById(id);

        Assert.NotNull(found);
        Assert.Equal(id, found!.Id);
        Assert.Equal(fullName, found.FullName);
        Assert.Equal(active, found.Active);
    }

    [Fact]
    public void GetAllUser_IncreasesAfterCreate()
    {
        var countUsers = _svc.GetAll().Count;
        _svc.Create(new Usr { Id = "10", FullName = "Jully",   Active = true });
        _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

        var users = _svc.GetAll();
        Assert.NotNull(users);
        Assert.Equal(countUsers + 2, users.Count);
    }

    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
