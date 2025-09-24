using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Db.MSSQL.EF.Entity;

namespace User.Db.MSSQL.EF.Models
{
    public interface IUsersSvc
    {
        List<UsrEtt> GetAll();
        UsrEtt? GetById(string id);
        string? Create(UsrEtt user);
        string? DelById(string id);
        string? Update(UsrEtt user);
        List<UsrEtt> GetByKwd(string keyword);
    }
    public class EFUsrsSvc : IUsersSvc
    {
        private readonly UsrDbContext _db;
        public EFUsrsSvc(UsrDbContext db)
        {
            _db = db;
        }

        public List<UsrEtt> GetAll()
        {
            return _db.Users.ToList();
        }

        public UsrEtt? GetById(string id)
        {
            var user = _db.Users.Find(id);
            return user;
        }

        public string? Create(UsrEtt usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id) || string.IsNullOrWhiteSpace(usr.FullName))
                return "Invalid user";
            if (_db.Users.Any(x => x.Id == usr.Id))
                return "User already exists";

            var userEntity = new UsrEtt
            {
                Id = usr.Id,
                FullName = usr.FullName,
                Address = usr.Address,
                BirthDay = usr.BirthDay,
                Description = usr.Description,
                Active = usr.Active
            };

            _db.Users.Add(userEntity);
            _db.SaveChanges();
            return null;
        }

        public string? DelById(string id)
        {
            var user = _db.Users.Find(id);
            if (user == null)
                return "User not found";
            _db.Users.Remove(user);
            _db.SaveChanges();
            return null;
        }

        public string? Update(UsrEtt usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id))
                return "Invalid user";
            var existingUser = _db.Users.Find(usr.Id);
            if (existingUser == null)
                return "User not found";

            existingUser.FullName = usr.FullName;
            existingUser.Address = usr.Address;
            existingUser.BirthDay = usr.BirthDay;
            existingUser.Description = usr.Description;
            existingUser.Active = usr.Active;

            _db.SaveChanges();
            return null;
        }

        public List<UsrEtt> GetByKwd(string keyword)
        {
            var matches = _db.Users
                .Where(x => x.FullName.Contains(keyword))
                .ToList();
            return matches;
        }
    }
}