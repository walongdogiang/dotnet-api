using NUnit.Framework;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Nun.Cases;

[TestFixture]
public sealed class NDelTests
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

    [TestCase("d01")]
    [TestCase("d02")]
    public void TestDelete(string id)
    {
        var newUser = new Usr { Id = id, FullName = "Temp", Active = true };
        var createMsg = _svc.Create(newUser);
        Assert.That(createMsg, Is.Null, "New user should be created without error.");

        var countBefore = _svc.GetAll().Count;
        var msg = _svc.DelById(id);

        Assert.That(msg, Is.Null, "Delete existing should return null.");
        Assert.That(_svc.GetAll().Count, Is.EqualTo(countBefore - 1), "User count should decrease by 1.");
        Assert.That(_svc.GetById(id), Is.Null, "Deleted user must not be found.");
    }

    [Test]
    public void TestDelete_NotFound()
    {
        var before = _svc.GetAll().Count;

        var msg = _svc.DelById("abc100");

        Assert.That(msg, Is.Not.Null, "Deleting non-existing should return error message.");
        Assert.That(_svc.GetAll().Count, Is.EqualTo(before), "Count must not change when deleting non-existing user.");
    }

    [Test]
    public void TestDeleteAll()
    {
        if (_svc.GetAll().Count == 0)
        {
            _svc.Create(new Usr { Id = "a1", FullName = "A", Active = true });
            _svc.Create(new Usr { Id = "a2", FullName = "B", Active = true });
            _svc.Create(new Usr { Id = "a3", FullName = "C", Active = true });
        }

        var list = _svc.GetAll();
        Assert.That(list, Is.Not.Null);
        Assert.That(list.Count, Is.GreaterThanOrEqualTo(3), "Seed should create at least 3 users.");

        foreach (var u in list.ToArray())
        {
            var del = _svc.DelById(u.Id);
            Assert.That(del, Is.Null, $"Delete [{u.Id} - {u.FullName}] should succeed.");
        }

        Assert.That(_svc.GetAll().Count, Is.EqualTo(0), "All users should be deleted.");
    }

    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
