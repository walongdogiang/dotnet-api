using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Test.Nun.BaseClass;

namespace User.Test.Nun.Db.MSSQL.Ao
{
    [TestFixture]
    public sealed class NDelTests : BaseNDelTests<AoUsrsSvc>
    {
        // Test method với TestCase
        [TestCase("d01")]
        [TestCase("d02")]
        public void TestDelete(string id)
        {
            Delete(id);
        }

        // Test delete user không tồn tại
        [Test]
        public void TestDelete_NotFound()
        {
            Delete_NotFound();
        }

        // Test delete tất cả users (duyệt và xóa từng user)
        [Test]
        public void TestDeleteAll()
        {
            DeleteAll();
        }
    }
}
