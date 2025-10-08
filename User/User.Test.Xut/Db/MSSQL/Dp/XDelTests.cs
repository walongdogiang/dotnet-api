using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Test.Xut.BaseClass;

namespace User.Test.Xut.Db.MSSQL.Dp
{
    public sealed class XDelTests : BaseXDelTests<DpUsrsSvc>
    {
        // Test method với Theory & InlineData
        [Theory]
        [InlineData("d01")]
        [InlineData("d02")]
        public void TestDelete(string id)
        {
            Delete(id);
        }

        // Test delete user không tồn tại
        [Fact]
        public void TestDelete_NotFound()
        {
            Delete_NotFound();
        }

        // Test delete tất cả users (duyệt và xóa từng user)
        [Fact]
        public void TestDeleteAll()
        {
            DeleteAll();
        }
    }
}
