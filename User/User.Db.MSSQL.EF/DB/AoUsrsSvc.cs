using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using User.Svc;

namespace User.Db.MSSQL.EF.DB
{
    public class AoUsrsSvc : BaseMapping, IUsersSvc
    {
        private readonly IDbConnectionFactory _factory;
        public AoUsrsSvc(IDbConnectionFactory factory) => _factory = factory;

        private const string Columns = "[Id],[FullName],[Address],[BirthDay],[Description],[Active]";

        public List<Usr> GetAll()
        {
            using var conn = _factory.Create();
            var sql = $"SELECT {Columns} FROM dbo.Users WITH (READCOMMITTEDLOCK)";
            
            var users = new List<Usr>();
            using var command = new SqlCommand(sql, (SqlConnection)conn);
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                users.Add(new Usr
                {
                    Id = reader["Id"].ToString(),
                    FullName = reader["FullName"].ToString(),
                    Address = reader["Address"]?.ToString(),
                    BirthDay = reader["BirthDay"] as DateTime? ?? DateTime.MinValue,
                    Description = reader["Description"]?.ToString(),
                    Active = Convert.ToBoolean(reader["Active"])
                });
            }
            
            return users;
        }

        public Usr? GetById(string id)
        {
            using var conn = _factory.Create();
            var sql = $"SELECT {Columns} FROM dbo.Users WHERE Id = @Id";
            
            using var command = new SqlCommand(sql, (SqlConnection)conn);
            command.Parameters.AddWithValue("@Id", id);
            
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Usr
                {
                    Id = reader["Id"].ToString(),
                    FullName = reader["FullName"].ToString(),
                    Address = reader["Address"]?.ToString(),
                    BirthDay = reader["BirthDay"] as DateTime? ?? DateTime.MinValue,
                    Description = reader["Description"]?.ToString(),
                    Active = Convert.ToBoolean(reader["Active"])
                };
            }
            
            return null;
        }

        public string? Create(Usr usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id) || string.IsNullOrWhiteSpace(usr.FullName))
                return "Invalid user";

            using var conn = _factory.Create();
            
            // Kiểm tra tồn tại
            var checkSql = "SELECT COUNT(1) FROM dbo.Users WHERE Id = @Id";
            using var checkCommand = new SqlCommand(checkSql, (SqlConnection)conn);
            checkCommand.Parameters.AddWithValue("@Id", usr.Id);
            
            var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
            if (exists) return "User already exists";

            var sql = $@"
    INSERT INTO dbo.Users ({Columns})
    VALUES (@Id, @FullName, @Address, @BirthDay, @Description, @Active);";
            
            using var command = new SqlCommand(sql, (SqlConnection)conn);
            command.Parameters.AddWithValue("@Id", usr.Id);
            command.Parameters.AddWithValue("@FullName", usr.FullName);
            command.Parameters.AddWithValue("@Address", usr.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BirthDay", usr.BirthDay);
            command.Parameters.AddWithValue("@Description", usr.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Active", usr.Active);
            
            command.ExecuteNonQuery();
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
            
            using var command = new SqlCommand(sql, (SqlConnection)conn);
            command.Parameters.AddWithValue("@Id", usr.Id);
            command.Parameters.AddWithValue("@FullName", usr.FullName);
            command.Parameters.AddWithValue("@Address", usr.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BirthDay", usr.BirthDay);
            command.Parameters.AddWithValue("@Description", usr.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Active", usr.Active);
            
            var rows = command.ExecuteNonQuery();
            return rows == 0 ? "User not found" : null;
        }

        public string? DelById(string id)
        {
            using var conn = _factory.Create();
            var sql = "DELETE FROM dbo.Users WHERE Id=@Id";
            
            using var command = new SqlCommand(sql, (SqlConnection)conn);
            command.Parameters.AddWithValue("@Id", id);
            
            var rows = command.ExecuteNonQuery();
            return rows == 0 ? "User not found" : null;
        }

        public List<Usr> GetByKwd(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new();
            
            using var conn = _factory.Create();
            var sql = $"SELECT {Columns} FROM dbo.Users WHERE FullName LIKE @kwd";
            
            var users = new List<Usr>();
            using var command = new SqlCommand(sql, (SqlConnection)conn);
            command.Parameters.AddWithValue("@kwd", $"%{keyword}%");
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new Usr
                {
                    Id = reader["Id"].ToString(),
                    FullName = reader["FullName"].ToString(),
                    Address = reader["Address"]?.ToString(),
                    BirthDay = reader["BirthDay"] as DateTime? ?? DateTime.MinValue,
                    Description = reader["Description"]?.ToString(),
                    Active = Convert.ToBoolean(reader["Active"])
                });
            }
            
            return users;
        }
    }
}
