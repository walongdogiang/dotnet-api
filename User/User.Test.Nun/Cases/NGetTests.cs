using NUnit.Framework;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Nun.Cases;

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

        _svc = services.BuildServiceProvider().GetRequiredService<IUsersSvc>();
    }

    [TestCase("1", "Nguyen Van A", true)]
    [TestCase("2", "Tran Thi B", false)]
    [TestCase("3", "Le Van C", true)]
    public void TestGetById(string id, string fullName, bool active)
    {
        var found = _svc.GetById(id);

        Assert.That(found, Is.Not.Null, $"User with ID '{id}' should exist.");
        Assert.That(found!.Id, Is.EqualTo(id), $"Id should match with '{id}'.");
        Assert.That(found.FullName, Is.EqualTo(fullName), $"Fullname should match with '{fullName}'.");
        Assert.That(found.Active, Is.EqualTo(active), $"Active status should match with '{active}'.");

        Console.WriteLine($"Info: Id={found.Id} | Fullname={found.FullName} | active={found.Active}");
    }

    [Test]
    public void TestGetAllUser()
    {
        var countUsers = _svc.GetAll().Count;
        _svc.Create(new Usr { Id = "10", FullName = "Jully",   Active = true });
        _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

        TestContext.WriteLine("TestGetAllUser");

        var users = _svc.GetAll();
        Assert.That(users, Is.Not.Null);
        Assert.That(users.Count, Is.EqualTo(countUsers + 2), "User count should increase by 2 after adding 2 users.");

        TestContext.WriteLine($"[COUNT] users = {users.Count}");
    }

    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
