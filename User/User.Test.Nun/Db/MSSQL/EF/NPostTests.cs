using NUnit.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;
using User.Test.Nun.BaseClass;

namespace User.Test.Nun.Db.MSSQL.EF
{
    [TestFixture]
    public sealed class NPostTests : BaseNPostTests<EFUsrsSvc>
    {
        [TestCase("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
        [TestCase("p02", "Tran Thi B", "456 Avenue, City", "1992-02-02", "Description for Tran Thi B", false)]
        public void TestCreate(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            Create(id, fullName, address, birthDay, description, active);
        }

        [Test]
        public void TestCreate_Duplicate()
        {
            Create_Duplicate();
        }
    }
}
