using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Db.MSSQL.EF.Entity;
using User.Svc;

namespace User.Db.MSSQL.EF.DB
{
    public interface IBaseMapping
    {
        Usr ToModel(UsrEtt e);
        UsrEtt ToEntity(Usr m);
        void Apply(Usr m, UsrEtt e);
    }

    public abstract class BaseMapping : IBaseMapping
    {
        public Usr ToModel(UsrEtt e) => new Usr
        {
            Id          = e.Id,
            FullName    = e.FullName,
            Address     = e.Address,
            BirthDay    = e.BirthDay,
            Description = e.Description,
            Active      = e.Active
        };

        public UsrEtt ToEntity(Usr m) => new UsrEtt
        {
            Id          = m.Id,
            FullName    = m.FullName,
            Address     = m.Address,
            BirthDay    = m.BirthDay,
            Description = m.Description,
            Active      = m.Active
        };

        public void Apply(Usr m, UsrEtt e)
        {
            e.FullName    = m.FullName;
            e.Address     = m.Address;
            e.BirthDay    = m.BirthDay;
            e.Description = m.Description;
            e.Active      = m.Active;
        }
    }
}