using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Db.MSSQL.EF.Entity;
using User.Svc;
using User.Test.Xut.BaseClass;

namespace User.Test.Xut.Db.MSSQL.EF
{
    public sealed class XPutTests : BaseXPutTests<EFUsrsSvc>
    {
        [Theory]
        [InlineData("u01", "Vu Hieu", "1 ABC Street", "1990-01-01", "First update", true)]
        [InlineData("u02", "Hong Tam", "2 DEF Avenue", "1992-02-02", "Second update", false)]
        [InlineData("u03", "5ilence", "3 GHI Road", "1995-03-03", "Third update", true)]
        public void TestUpdate(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            Update(id, fullName, address, birthDay, description, active);
        }

        // 2) Update user tồn tại
        [Fact]
        public void TestUpdate_ExistingUser_Succeeds_And_ChangesAllFields()
        {
            Update_ExistingUser_Succeeds_And_ChangesAllFields();
        }

        // 3) Update user không tồn tại
        [Fact]
        public void TestUpdate_NotFound_ReturnsError_And_DoesNotCreate()
        {
            Update_NotFound_ReturnsError_And_DoesNotCreate();
        }
    }
}
