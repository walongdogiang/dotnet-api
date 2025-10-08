using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;
using Dapper;

namespace User.Db.MSSQL.EF.DB
{
    public class DpUsrsSvc : BaseMapping, IUsersSvc
    {
        private readonly IDbConnectionFactory _factory;
        public DpUsrsSvc(IDbConnectionFactory factory) => _factory = factory;

        private const string Columns = "[Id],[FullName],[Address],[BirthDay],[Description],[Active]";

        public List<Usr> GetAll()
        {
            using var conn = _factory.Create();
            var sql = $"SELECT {Columns} FROM dbo.Users WITH (READCOMMITTEDLOCK)";
            return conn.Query<Usr>(sql).ToList();
        }

        public Usr? GetById(string id)
        {
            using var conn = _factory.Create();
            var sql = $"SELECT {Columns} FROM dbo.Users WHERE Id = @Id";
            return conn.QueryFirstOrDefault<Usr>(sql, new { Id = id });
        }

        public string? Create(Usr usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id) || string.IsNullOrWhiteSpace(usr.FullName))
                return "Invalid user";

            using var conn = _factory.Create();
            // kiểm tra tồn tại
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.Users WHERE Id = @Id", new { usr.Id }) > 0;
            if (exists) return "User already exists";

            var sql = $@"
    INSERT INTO dbo.Users ({Columns})
    VALUES (@Id, @FullName, @Address, @BirthDay, @Description, @Active);";
            conn.Execute(sql, usr);
            return null;
        }

        public string? Update(Usr usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id))
                return "Invalid user";

            using var conn = _factory.Create();
            var sql = @"
    UPDATE dbo.Users
    SET FullName=@FullName, Address=@Address, BirthDay=@BirthDay, Description=@Description, Active=@Active
    WHERE Id=@Id;";
            var rows = conn.Execute(sql, usr);
            return rows == 0 ? "User not found" : null;
        }

        public string? DelById(string id)
        {
            using var conn = _factory.Create();
            var rows = conn.Execute("DELETE FROM dbo.Users WHERE Id=@Id", new { Id = id });
            return rows == 0 ? "User not found" : null;
        }

        public List<Usr> GetByKwd(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new();
            using var conn = _factory.Create();

            // Contains: '%kwd%' (chậm nếu data lớn). Nếu muốn index-friendly, đổi sang StartsWith: kwd + '%'.
            var sql = $"SELECT {Columns} FROM dbo.Users WHERE FullName LIKE @kwd";
            return conn.Query<Usr>(sql, new { kwd = $"%{keyword}%" }).ToList();
        }
    }
}