using Microsoft.EntityFrameworkCore;
using User.Svc;                    // IUsersSvc, Usr (model trả ra ngoài)
using User.Db.MSSQL.EF.Entity;     // UsrEtt (entity)
using System;
using System.Collections.Generic;
using System.Linq;

namespace User.Db.MSSQL.EF.DB
{
    // EF implement interface OUTPUT chung: IUsersSvc (trả Usr)
    public sealed class EFUsrsSvc : BaseMapping, IUsersSvc
    {
        private readonly UsrDbContext _db;
        public EFUsrsSvc(UsrDbContext db) => _db = db;

        // ================= CRUD ===================

        public List<Usr> GetAll()
        {
            return _db.Users
                      .AsNoTracking()
                      .Select(e => ToModel(e))
                      .ToList();
        }

        public Usr? GetById(string id)
        {
            var e = _db.Users.AsNoTracking().FirstOrDefault(x => x.Id == id);
            return e is null ? null : ToModel(e);
        }

        public string? Create(Usr usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id) || string.IsNullOrWhiteSpace(usr.FullName))
                return "Invalid user";

            var exists = _db.Users.Any(x => x.Id == usr.Id);
            if (exists) return "User already exists";

            _db.Users.Add(ToEntity(usr));
            _db.SaveChanges();
            return null;
        }

        public string? DelById(string id)
        {
            var e = _db.Users.Find(id);
            if (e is null) return "User not found";

            _db.Users.Remove(e);
            _db.SaveChanges();
            return null;
        }

        public string? Update(Usr user)
        {
            if (user is null || string.IsNullOrWhiteSpace(user.Id))
                return "Invalid user";

            var e = _db.Users.Find(user.Id);
            if (e is null) return "User not found";

            Apply(user, e);
            _db.SaveChanges();
            return null;
        }

        public List<Usr> GetByKwd(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<Usr>();
            return _db.Users
                      .AsNoTracking()
                      .Where(x => x.FullName.Contains(keyword))
                      .Select(e => ToModel(e))
                      .ToList();
        }
    }
}
