using NUnit.Framework;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Nun.Cases;

[TestFixture]
public sealed class NPostTests
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

    [TestCase("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
    [TestCase("p02", "Tran Thi B",   "456 Avenue, City", "1992-02-02", "Description for Tran Thi B",   false)]
    public void TestCreate(string id, string fullName, string address, string birthDay, string description, bool active)
    {
        var countBefore = _svc.GetAll().Count;

        var msg = _svc.Create(new Usr {
            Id = id, FullName = fullName, Address = address,
            BirthDay = DateTime.Parse(birthDay), Description = description, Active = active
        });

        var countAfter = _svc.GetAll().Count;
        Assert.That(msg, Is.Null, "Create should return null on success.");

        var found = _svc.GetById(id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(id));
        Assert.That(found.FullName, Is.EqualTo(fullName));
        Assert.That(found.Active, Is.EqualTo(active));

        Assert.That(countAfter, Is.EqualTo(countBefore + 1));
        TestContext.WriteLine($"Test Create: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
    }

    [Test]
    public void TestCreate_Duplicate()
    {
        var id = "dup01";
        var msg1 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 1", Active = true });
        Assert.That(msg1, Is.Null, "First create should succeed.");

        var count = _svc.GetAll().Count;
        var msg2 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 2", Active = false });

        Assert.That(msg2, Is.Not.Null, "Second create with duplicate ID should return error message.");
        Assert.That(_svc.GetAll().Count, Is.EqualTo(count));
    }

    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
