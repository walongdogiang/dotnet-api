using Xunit;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Xun.Cases;

public sealed class XDelTests
{
    private readonly IUsersSvc _svc;

    public XDelTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITimeProvider, SystemTimeProvider>()
            .AddSingleton<IUsersSvc, UsersSvc>();

        _svc = services.BuildServiceProvider().GetRequiredService<IUsersSvc>();
    }

    [Theory]
    [InlineData("d01")]
    [InlineData("d02")]
    public void Delete_Existing_ReturnsNull_And_DecreaseCount(string id)
    {
        var newUser = new Usr { Id = id, FullName = "Temp", Active = true };
        var createMsg = _svc.Create(newUser);
        Assert.True(string.IsNullOrEmpty(createMsg), "New user should be created without error.");

        var countBefore = _svc.GetAll().Count;
        var msg = _svc.DelById(id);

        Assert.Null(msg);
        Assert.Equal(countBefore - 1, _svc.GetAll().Count);
        Assert.Null(_svc.GetById(id));
    }

    [Fact]
    public void Delete_NotFound_ReturnsError_And_NotChangeCount()
    {
        var before = _svc.GetAll().Count;
        var msg = _svc.DelById("abc100");

        Assert.False(string.IsNullOrEmpty(msg));
        Assert.Equal(before, _svc.GetAll().Count);
    }

    [Fact]
    public void DeleteAll_RemovesAllUsers()
    {
        if (_svc.GetAll().Count == 0)
        {
            _svc.Create(new Usr { Id = "a1", FullName = "A", Active = true });
            _svc.Create(new Usr { Id = "a2", FullName = "B", Active = true });
            _svc.Create(new Usr { Id = "a3", FullName = "C", Active = true });
        }

        var list = _svc.GetAll();
        Assert.NotNull(list);
        Assert.True(list.Count >= 3, "Seed should create at least 3 users.");

        foreach (var u in list.ToArray())
        {
            var del = _svc.DelById(u.Id);
            Assert.True(string.IsNullOrEmpty(del), $"Delete [{u.Id} - {u.FullName}] should succeed.");
        }

        Assert.Equal(0, _svc.GetAll().Count);
    }

    // local shim để khớp DI
    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
