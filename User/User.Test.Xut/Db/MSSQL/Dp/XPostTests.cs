using Xunit;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;
using User.Test.Xut.BaseClass;

namespace User.Test.Xut.Db.MSSQL.Dp
{
    public sealed class XPostTests : BaseXPostTests<DpUsrsSvc>
    {
        [Theory]
        [InlineData("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
        [InlineData("p02", "Tran Thi B", "456 Avenue, City", "1992-02-02", "Description for Tran Thi B", false)]
        public void TestCreate(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            Create(id, fullName, address, birthDay, description, active);
        }

        [Fact]
        public void TestCreate_Duplicate()
        {
            Create_Duplicate();
        }
    }
}
