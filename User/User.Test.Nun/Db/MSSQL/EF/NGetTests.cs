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
    public sealed class NGetTests : BaseNGetTests<EFUsrsSvc>
    {
        [TestCase("1", "Nguyen Van A", true)]
        [TestCase("2", "Tran Thi B", false)]
        [TestCase("3", "Le Van C", true)]
        public void TestGetById(string id, string fullName, bool active)
        {
            GetById(id, fullName, active);
        }
        
        [Test]
        public void TestGetAll()
        {
            GetAllUser();
        }

        [TestCase("Nguyen", 1)]
        [TestCase("Tran", 1)]
        [TestCase("Le", 1)]
        [TestCase("Van", 2)]
        [TestCase("NonExistent", 0)]
        public void TestGetByKwd(string keyword, int expectedCount)
        {
            GetByKwd(keyword, expectedCount);
        }
    }
}
