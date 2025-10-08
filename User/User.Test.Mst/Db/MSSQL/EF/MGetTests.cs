using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;
using User.Test.Mst.BaseClass;

namespace User.Test.Mst.Db.MSSQL.EF
{
    [TestClass]
    public sealed class MGetTests : BaseMGetTests<EFUsrsSvc>
    {
        [DataTestMethod]
        [DataRow("1", "Nguyen Van A", true)]
        [DataRow("2", "Tran Thi B", false)]
        [DataRow("3", "Le Van C", true)]
        public void TestGetById(string id, string fullName, bool active)
        {
            GetById(id, fullName, active);
        }
        [TestMethod]
        public void TestGetAll()
        {
            GetAllUser();
        }

        [DataTestMethod]
        [DataRow("Nguyen", 1)]
        [DataRow("Tran", 1)]
        [DataRow("Le", 1)]
        [DataRow("Van", 2)]
        [DataRow("NonExistent", 0)]
        public void TestGetByKwd(string keyword, int expectedCount)
        {
            GetByKwd(keyword, expectedCount);
        }
    }
}
