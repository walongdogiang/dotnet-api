using Xunit;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Xun.Cases;

public sealed class XPostTests
{
    private readonly IUsersSvc _svc;

    public XPostTests()
    {
        var services = new ServiceCollection()
            .AddSingleton<ITimeProvider, SystemTimeProvider>()
            .AddSingleton<IUsersSvc, UsersSvc>();

        _svc = services.BuildServiceProvider().GetRequiredService<IUsersSvc>();
    }

    [Theory]
    [InlineData("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
    [InlineData("p02", "Tran Thi B",   "456 Avenue, City", "1992-02-02", "Description for Tran Thi B",   false)]
    public void Create_Succeeds(string id, string fullName, string address, string birthDay, string description, bool active)
    {
        var countBefore = _svc.GetAll().Count;

        var msg = _svc.Create(new Usr {
            Id = id, FullName = fullName, Address = address,
            BirthDay = DateTime.Parse(birthDay), Description = description, Active = active
        });

        var countAfter = _svc.GetAll().Count;

        Assert.True(string.IsNullOrEmpty(msg));
        var found = _svc.GetById(id);
        Assert.NotNull(found);
        Assert.Equal(id, found!.Id);
        Assert.Equal(fullName, found.FullName);
        Assert.Equal(active, found.Active);
        Assert.Equal(countBefore + 1, countAfter);
    }

    [Fact]
    public void Create_Duplicate_ReturnsError_And_NotChangeCount()
    {
        var id = "dup01";
        var msg1 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 1", Active = true });
        Assert.True(string.IsNullOrEmpty(msg1));

        var count = _svc.GetAll().Count;
        var msg2 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 2", Active = false });

        Assert.False(string.IsNullOrEmpty(msg2));
        Assert.Equal(count, _svc.GetAll().Count);
    }

    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
