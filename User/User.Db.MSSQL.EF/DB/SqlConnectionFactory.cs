using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace User.Db.MSSQL.EF.DB
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }

    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _cs;
        public SqlConnectionFactory(string connectionString) => _cs = connectionString;

        public IDbConnection Create()
        {
            var conn = new SqlConnection(_cs);
            conn.Open();
            return conn;
        }
    }
}