using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using User.Db.MSSQL.EF;
using User.Svc;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        protected IUsersSvc _svc;
        
        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();

            services.AddDbContext<UsrDbContext>(opt =>
                opt.UseInMemoryDatabase($"mst-ef-{Guid.NewGuid()}"));

            // Đăng ký EFUsrsSvc thay vì UsersSvc
            services.AddSingleton<ITimeProvider, SystemTimeProvider>().AddSingleton<IUsersSvc, TSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<IUsersSvc>();
        }
        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}
