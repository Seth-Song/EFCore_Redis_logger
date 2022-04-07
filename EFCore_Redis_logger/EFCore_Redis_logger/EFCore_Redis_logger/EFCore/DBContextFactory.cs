using Microsoft.EntityFrameworkCore;

namespace EFCore_Redis_logger.EFCore
{
    //DBContext 线程安全问题，为每个线程生成一个DBContext
    public class DBContextFactory
    {
        private readonly IDbContextFactory<DBContext> _contextFactory;
        public DBContextFactory(IDbContextFactory<DBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public DBContext CreateDbContext()
        {
            return _contextFactory.CreateDbContext();
        }

        public IDBRepository GetDBRepository(DBContext context)
        {
            IDBRepository coreDBRepository = new DBRepository(context);
            return coreDBRepository;
        }
    }
}
