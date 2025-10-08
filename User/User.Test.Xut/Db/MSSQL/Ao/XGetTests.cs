using Xunit;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;
using User.Test.Xut.BaseClass;

namespace User.Test.Xut.Db.MSSQL.Ao
{
    public sealed class XGetTests : BaseXGetTests<AoUsrsSvc>
    {
        [Theory]
        [InlineData("1", "Nguyen Van A", true)]
        [InlineData("2", "Tran Thi B", false)]
        [InlineData("3", "Le Van C", true)]
        public void TestGetById(string id, string fullName, bool active)
        {
            GetById(id, fullName, active);
        }
        
        [Fact]
        public void TestGetAll()
        {
            GetAllUser();
        }

        [Theory]
        [InlineData("Nguyen", 1)]
        [InlineData("Tran", 1)]
        [InlineData("Le", 1)]
        [InlineData("Van", 2)]
        [InlineData("NonExistent", 0)]
        public void TestGetByKwd(string keyword, int expectedCount)
        {
            GetByKwd(keyword, expectedCount);
        }
    }
}
