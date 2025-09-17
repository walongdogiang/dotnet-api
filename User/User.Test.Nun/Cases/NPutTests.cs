using NUnit.Framework;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Nun.Cases;

[TestFixture]
public sealed class NPutTests
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

    // ====== 1) Test tham số hóa (Parameterized) ======
    [TestCase("u01", "Vu Hieu",  "1 ABC Street", "1990-01-01", "First update",  true)]
    [TestCase("u02", "Hong Tam", "2 DEF Avenue", "1992-02-02", "Second update", false)]
    [TestCase("u03", "5ilence",  "3 GHI Road",   "1995-03-03", "Third update",  true)]
    public void TestUpdate_AllFields_Verified(string id, string fullName, string address, string birthDay, string description, bool active)
    {
        // Seed nếu chưa có
        if (_svc.GetById(id) is null)
        {
            var seedMsg = _svc.Create(new Usr
            {
                Id = id, FullName = "Seed Name", Address = "Seed Addr",
                BirthDay = new DateTime(1980, 1, 1), Description = "Seed Desc", Active = !active
            });
            Assert.That(seedMsg, Is.Null.Or.Empty, $"Seeding '{id}' should succeed.");
        }

        var before = _svc.GetAll().Count;

        // Act
        var msg = _svc.Update(new Usr
        {
            Id = id,
            FullName = fullName,
            Address = address,
            BirthDay = DateTime.Parse(birthDay),
            Description = description,
            Active = active
        });

        // Assert
        Assert.That(msg, Is.Null.Or.Empty, "Update should return null/empty on success.");

        var found = _svc.GetById(id);
        Assert.That(found, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(found!.Id,          Is.EqualTo(id));
            Assert.That(found.FullName,     Is.EqualTo(fullName));
            Assert.That(found.Address,      Is.EqualTo(address));
            Assert.That(found.BirthDay,     Is.EqualTo(DateTime.Parse(birthDay)));
            Assert.That(found.Description,  Is.EqualTo(description));
            Assert.That(found.Active,       Is.EqualTo(active));
            Assert.That(_svc.GetAll().Count, Is.EqualTo(before)); // update không đổi tổng số bản ghi
        });
    }

    // ====== 2) Update user tồn tại (trường hợp riêng)
    [Test]
    public void Update_ExistingUser_Succeeds_And_ChangesAllFields()
    {
        const string id = "u100";
        _svc.DelById(id); // reset
        var created = _svc.Create(new Usr
        {
            Id = id, FullName = "Old Name", Address = "Old Addr",
            BirthDay = new DateTime(1999, 9, 9), Description = "Old Desc", Active = false
        });
        Assert.That(created, Is.Null.Or.Empty);

        var msg = _svc.Update(new Usr
        {
            Id = id,
            FullName = "New Name",
            Address = "New Addr",
            BirthDay = new DateTime(2000, 1, 1),
            Description = "New Desc",
            Active = true
        });
        Assert.That(msg, Is.Null.Or.Empty);

        var found = _svc.GetById(id);
        Assert.That(found, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(found!.FullName,   Is.EqualTo("New Name"));
            Assert.That(found.Address,     Is.EqualTo("New Addr"));
            Assert.That(found.BirthDay,    Is.EqualTo(new DateTime(2000, 1, 1)));
            Assert.That(found.Description, Is.EqualTo("New Desc"));
            Assert.That(found.Active,      Is.True);
        });
    }

    // ====== 3) Update user không tồn tại
    [Test]
    public void Update_NotFound_ReturnsError_And_DoesNotCreate()
    {
        const string id = "u404";
        _svc.DelById(id);
        var before = _svc.GetAll().Count;

        var msg = _svc.Update(new Usr
        {
            Id = id,
            FullName = "Ghost",
            Address = "No Addr",
            BirthDay = new DateTime(1970, 1, 1),
            Description = "Should fail",
            Active = false
        });

        Assert.That(msg, Is.Not.Null.And.Not.Empty);
        Assert.That(_svc.GetById(id), Is.Null);
        Assert.That(_svc.GetAll().Count, Is.EqualTo(before));
    }

    public interface ITimeProvider { DateTime Now { get; } }
    public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
}
