using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static EFCore_Redis_logger.Models.WebPageInfo;

namespace EFCore_Redis_logger.EFCore
{
    public interface IDBRepository : IDisposable
    {
        public DbContext Context { get; set; }

        #region Create
        T Add<T>(T entity, bool withSaveChange = true) where T : class;
        IEnumerable<T> AddRange<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class;
        int AddRangeReturnCount<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class;
        T AddHandleConcurrency<T>(T entity, bool withSaveChange = true) where T : class;
        #endregion

        #region Retrieve
        #region Single & FirstOrDefault
        T Single<T>(Expression<Func<T, bool>> expression, bool isTracking = false) where T : class;
        T FirstOrDefault<T>(Expression<Func<T, bool>> expression, bool isTracking = false) where T : class;
        #endregion
        #region All
        DbSet<T> GetDbSet<T>() where T : class;
        IQueryable<T> AllWithTracking<T>(Expression<Func<T, bool>> expression = null) where T : class;
        IQueryable<T> AllWithNoTracking<T>(Expression<Func<T, bool>> expression = null) where T : class;
        IQueryable<T> All<T>(Expression<Func<T, bool>> expression = null, bool isTracking = false) where T : class;
        #endregion
        #region Condition
        bool Any<T>(Expression<Func<T, bool>> expression = null) where T : class;
        int Count<T>(Expression<Func<T, bool>> expression = null) where T : class;

        bool Contains<T>(Expression<Func<T, bool>> expression = null) where T : class;
        #endregion
        #region Find
        T Find<T>(Guid id) where T : class;
        T Find<T>(params object[] keys) where T : class;
        T Find<T>(Expression<Func<T, bool>> expression) where T : class;
        IQueryable<T> FindWithOrderBy<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class;
        IQueryable<T> FindWithOrderByDescending<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class;
        #endregion
        #region Get
        TResult GetOneWithSelector<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector) where T : class;
        IQueryable<TResult> GetAllWithSelector<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector) where T : class;
        TableInfo<T> GetAllWithPaging<T, TResult>(TableQueryParameter<T, TResult> queryParameter) where T : class;
        #endregion
        #region Filter
        IQueryable<T> Filter<T>(Expression<Func<T, bool>> expression, bool isTracking = false) where T : class;
        IQueryable<T> FilterWithTracking<T>(Expression<Func<T, bool>> expression) where T : class;
        IQueryable<T> FilterWithNoTracking<T>(Expression<Func<T, bool>> expression) where T : class;
        IQueryable<TResult> Filter<T, TResult>(Expression<Func<T, TResult>> selector, bool isTracking = false) where T : class;
        IQueryable<TResult> Filter<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector, bool isTracking = false) where T : class;
        IQueryable<T> FilterPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null, bool isTracking = false) where T : class;
        IQueryable<T> FilterWithNavProps<T>(Expression<Func<T, bool>> expression, List<Expression<Func<T, object>>> navProps = null, bool isTracking = false) where T : class;

        T FirstOrDefaultPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null, bool isTracking = false) where T : class;
        IQueryable<T> FilterWithOrderBy<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order, bool isTracking = false) where T : class;
        IQueryable<T> FilterWithOrderByDescending<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order, bool isTracking = false) where T : class;
        IQueryable<T> Filter<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> orderBy, bool orderByASC, out int pageCount, int pageIndex, int pageSize, bool isTracking = false) where T : class;
        IQueryable<T> Filter<T>(Expression<Func<T, bool>> expression, out int total, int index = 0, int size = 50, bool isTracking = false) where T : class;
        #endregion

        void LoadNavigationProperty<T>(T TObject, string navigationProperty) where T : class;
        #endregion

        #region Update
        int UpdateWithKey<T>(T TObject, string key = "Id", bool withSaveChange = true) where T : class;
        int UpdateRangeWithKey<T>(IEnumerable<T> TObjects, string key = "Id", bool withSaveChange = true) where T : class;
        int Update<T>(T entity, bool withSaveChange = true) where T : class;
        int UpdateRange<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class;
        #endregion

        #region Delete
        T DeleteReturnEntity<T>(T entity, bool withSaveChange = true) where T : class;
        int Delete<T>(T entity, bool withSaveChange = true) where T : class;
        IEnumerable<T> DeleteRangeReturnEntities<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class;
        int DeleteRange<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class;
        int Delete<T>(Expression<Func<T, bool>> expression, bool withSaveChange = true) where T : class;
  
        #endregion

        #region BySql
        IQueryable<T> FromSql<T>(string sql, params object[] param) where T : class;
        IQueryable<T> FromSql<T>(string formattedSql) where T : class;
        List<T> SqlQuery<T>(string sql, params object[] param);
        List<T> SqlQueryWithDBOpen<T>(string sql, params object[] param);

        int ExecuteSqlCommand(string sql, params object[] param);
        int ExecuteSqlCommand(string formattedSql);

        int BatchExecuteSqlCommand(List<string> sqls);

        #endregion

        #region Other
        void EagerInitDatabase();
        void Execute(Action<DbContext> action);
        void Attach<T>(T obj) where T : class;

        void DisableAutoDetectChanges();
        void EnableAutoDetectChanges();
        void DetectChanges();
        void ClearTracker();
        void SetCommandTimeout(int? second);
        void EnableLazyLoad();
        void DisableLazyLoad();
        #endregion

        #region Commit
        int Commit();
        int CommitHandleConcurrency();
        int SaveChanges(bool trackChange = true);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        #endregion

        


        #region NoLock
        T SingleWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        T FirstOrDefaultWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        List<T> AllWithNoLock<T>() where T : class;
        bool AnyWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        bool ContainsWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        T FindWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        T FindWithNoLockPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null) where T : class;
        T FindWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class;
        List<T> FilterWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        List<T> FilterWithNoTrackingWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class;
        List<TResult> FilterWithNoLock<T, TResult>(Expression<Func<T, TResult>> expression) where T : class;
        List<TResult> FilterWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector) where T : class;
        List<T> FilterWithNoLockPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null) where T : class;
        List<T> FilterWithNoLockPure<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class;
        List<T> FilterWithOrderByWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class;
        List<T> FilterWithOrderByDescendingWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order, bool isTracting = false) where T : class;
        List<T> FilterWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> orderBy, bool orderByASC, out int pageCount, int pageIndex, int pageSize, List<string> navProps = null) where T : class;
        #endregion
    }
}
