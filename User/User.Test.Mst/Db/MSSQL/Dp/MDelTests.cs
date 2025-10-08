using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Test.Mst.BaseClass;

namespace User.Test.Mst.Db.MSSQL.Dp
{
    [TestClass]
    public sealed class MDelTests : BaseMDelTests<DpUsrsSvc>
    {
        // Test method với DataTestMethod & DataRow
        [DataTestMethod]
        [DataRow("d01")]
        [DataRow("d02")]
        public void TestDelete(string id)
        {
            Delete(id);
        }

        // Test delete user không tồn tại
        [TestMethod]
        public void TestDelete_NotFound()
        {
            Delete_NotFound();
        }

        // Test delete tất cả users (duyệt và xóa từng user)
        [TestMethod]
        public void TestDeleteAll()
        {
            DeleteAll();
        }
    }
}