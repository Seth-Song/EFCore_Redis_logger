using Microsoft.EntityFrameworkCore;

namespace EFCore_Redis_logger.EFCore
{
    public class DBContext: DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
            //this.Database.SetCommandTimeout(600);
        }

    


    }
}
