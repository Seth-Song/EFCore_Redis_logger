using EFCore_Redis_logger.Utility.log;
using LinqToDB.Common;
using LinqToDB.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using static EFCore_Redis_logger.Models.WebPageInfo;

namespace EFCore_Redis_logger.EFCore
{
    public class DBRepository : IDBRepository
    {
        private static readonly DemoLogger logger = DemoLogger.GetInstance(typeof(DBRepository));

        #region Properties
        public DbContext Context { get; set; }
        #endregion

        public DBRepository(DbContext context)
        {
            Context = context;
        }

        #region Create

        public virtual T Add<T>(T entity, bool withSaveChange = true) where T : class
        {
            var entry = Context.Set<T>().Add(entity);
            SaveChangesInternal(withSaveChange, 1);
            return entry.Entity;
        }
        public virtual IEnumerable<T> AddRange<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class
        {
      
            Context.Set<T>().AddRange(entities);
            SaveChangesInternal(withSaveChange, entities.Count());
            return entities;
        }

        public virtual int AddRangeReturnCount<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class
        {
            Context.Set<T>().AddRange(entities);
            var result = SaveChangesInternal(withSaveChange, entities.Count());
            return result;
        }

        public virtual T AddHandleConcurrency<T>(T entity, bool withSaveChange = true) where T : class
        {
            var newEntry = Context.Set<T>().Add(entity);
            if (withSaveChange)
            {
                this.CommitHandleConcurrency();
            }
            return newEntry as T;
        }
        #endregion

        #region Retrieve
        #region Single & FirstOrDefault
        public virtual T Single<T>(Expression<Func<T, bool>> expression, bool isTracking = false) where T : class
        {
            return isTracking
                ? Context.Set<T>().Single(expression)
                : Context.Set<T>().AsNoTracking().Single(expression);
        }

        public virtual T FirstOrDefault<T>(Expression<Func<T, bool>> expression, bool isTracking = false) where T : class
        {
            return isTracking
                ? Context.Set<T>().FirstOrDefault(expression)
                : Context.Set<T>().AsNoTracking().FirstOrDefault(expression);
        }
        #endregion

        #region All
        public DbSet<T> GetDbSet<T>() where T : class
        {
            return Context.Set<T>();
        }

        public virtual IQueryable<T> AllWithTracking<T>(Expression<Func<T, bool>> expression = null) where T : class
        {
            var entities = expression == null
                ? Context.Set<T>().AsQueryable<T>()
                : Context.Set<T>().Where(expression).AsQueryable<T>();
            return entities;
        }

        public virtual IQueryable<T> AllWithNoTracking<T>(Expression<Func<T, bool>> expression = null) where T : class
        {
            var entities = expression == null
                ? Context.Set<T>().AsNoTracking().AsQueryable<T>()
                : Context.Set<T>().AsNoTracking().Where(expression).AsQueryable<T>();
            return entities;
        }

        public virtual IQueryable<T> All<T>(Expression<Func<T, bool>> expression = null, bool isTracking = false) where T : class
        {
            return isTracking
                ? this.AllWithTracking(expression)
                : this.AllWithNoTracking(expression);
        }
        #endregion

        #region Condition
        public virtual bool Any<T>(Expression<Func<T, bool>> expression = null) where T : class
        {
            var returnValue = expression == null
                ? Context.Set<T>().Any<T>()
                : Context.Set<T>().AsNoTracking().Any<T>(expression);
            return returnValue;
        }

        public virtual bool Contains<T>(Expression<Func<T, bool>> expression = null) where T : class
        {
            var returnValue = expression == null
                ? Context.Set<T>().Any<T>()
                : Context.Set<T>().AsNoTracking().Any<T>(expression);
            return returnValue;
            //return Context.Set<T>().Count<T>(predicate) > 0;
        }

        public virtual int Count<T>(Expression<Func<T, bool>> expression = null) where T : class
        {
            var returnValue = expression == null
                ? Context.Set<T>().Count<T>()
                : Context.Set<T>().AsNoTracking().Count<T>(expression);
            return returnValue;
        }
        #endregion

        #region Find
        public virtual T Find<T>(Guid id) where T : class
        {
            return Context.Set<T>().Find(id);
        }

        public virtual T Find<T>(params object[] keys) where T : class
        {
            return (T)Context.Set<T>().Find(keys);
        }

        public virtual T Find<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return Context.Set<T>().FirstOrDefault<T>(expression);
        }

        public virtual IQueryable<T> FindWithOrderBy<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class
        {
            return Context.Set<T>().Where<T>(expression).OrderBy<T, TResult>(order);
        }

        public virtual IQueryable<T> FindWithOrderByDescending<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class
        {
            return Context.Set<T>().Where<T>(expression).OrderByDescending<T, TResult>(order);
        }
        #endregion

        #region Get
        public virtual TResult GetOneWithSelector<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector) where T : class
        {
            return Context.Set<T>().Where(expression).Select(selector).FirstOrDefault();
        }

        public virtual IQueryable<TResult> GetAllWithSelector<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector) where T : class
        {
            return Context.Set<T>().Where(expression).Select(selector);
        }

        public virtual TableInfo<T> GetAllWithPaging<T, TResult>(TableQueryParameter<T, TResult> queryParameter) where T : class
        {
            TableInfo<T> resut = new TableInfo<T>();
            var dbSet = Context.Set<T>();
            int skipCount = (queryParameter.Pager.Index - 1) * queryParameter.Pager.Size;
            IOrderedQueryable<T> allDatas;
            if (queryParameter.Sorter.IsAscending)
            {
                allDatas = queryParameter.Filter != null ? dbSet.Where<T>(queryParameter.Filter).OrderBy(queryParameter.Sorter.SortBy) : dbSet.OrderBy(queryParameter.Sorter.SortBy);
                if (queryParameter.Sorter.ThenSortBy != null)
                {
                    allDatas = allDatas.ThenBy(queryParameter.Sorter.ThenSortBy);
                }
            }
            else
            {
                allDatas = queryParameter.Filter != null ? dbSet.Where<T>(queryParameter.Filter).OrderByDescending(queryParameter.Sorter.SortBy) : dbSet.OrderByDescending(queryParameter.Sorter.SortBy);
                if (queryParameter.Sorter.ThenSortBy != null)
                {
                    allDatas = allDatas.ThenByDescending(queryParameter.Sorter.ThenSortBy);
                }
            }
            var allCount = resut.TotalItemsCount = allDatas.Count();
            resut.PageCount = allCount == 0 ? 1 : (allCount % queryParameter.Pager.Size == 0 ? (allCount / queryParameter.Pager.Size) : (allCount / queryParameter.Pager.Size) + 1);
            resut.Items = skipCount == 0 ? allDatas.Take(queryParameter.Pager.Size).ToList() : allDatas.Skip(skipCount).Take(queryParameter.Pager.Size).ToList();
            return resut;
        }
        #endregion

        #region Filter
        public virtual IQueryable<T> Filter<T>(Expression<Func<T, bool>> expression, bool isTracking = false) where T : class
        {
            return isTracking
                ? this.FilterWithTracking(expression)
                : this.FilterWithNoTracking(expression);
        }

        public virtual IQueryable<T> FilterWithTracking<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return Context.Set<T>().Where<T>(expression).AsQueryable<T>();
        }

        public virtual IQueryable<T> FilterWithNoTracking<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return Context.Set<T>().Where<T>(expression).AsNoTracking<T>().AsQueryable<T>();
        }

        public virtual IQueryable<TResult> Filter<T, TResult>(Expression<Func<T, TResult>> selector, bool isTracking = false) where T : class
        {
            return isTracking
                ? Context.Set<T>().Select<T, TResult>(selector).AsQueryable<TResult>()
                : Context.Set<T>().AsNoTracking<T>().Select<T, TResult>(selector).AsQueryable<TResult>();
        }

        public virtual IQueryable<TResult> Filter<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector, bool isTracking = false) where T : class
        {
            return isTracking
                ? Context.Set<T>().Where<T>(expression).Select<T, TResult>(selector).AsQueryable<TResult>()
                : Context.Set<T>().Where<T>(expression).AsNoTracking<T>().Select<T, TResult>(selector).AsQueryable<TResult>();
        }

        public virtual IQueryable<T> FilterPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null, bool isTracking = false) where T : class
        {
            if (navProps == null)
            {
                return isTracking
                    ? Context.Set<T>().Where<T>(expression).AsQueryable<T>()
                    : Context.Set<T>().Where<T>(expression).AsNoTracking<T>().AsQueryable<T>();
            }
            else
            {
                return isTracking
                    ? GetDbSetIncludeNavigationProperties<T>(navProps).Where<T>(expression).AsQueryable<T>()
                    : GetDbSetIncludeNavigationProperties<T>(navProps).Where<T>(expression).AsNoTracking<T>().AsQueryable<T>();
            }
        }

        public virtual IQueryable<T> FilterWithNavProps<T>(Expression<Func<T, bool>> expression, List<Expression<Func<T, object>>> navProps = null, bool isTracking = false) where T : class
        {
            if (navProps == null)
            {
                return isTracking
                    ? Context.Set<T>().Where<T>(expression).AsQueryable<T>()
                    : Context.Set<T>().Where<T>(expression).AsNoTracking<T>().AsQueryable<T>();
            }
            else
            {
                return isTracking
                    ? GetDbSetIncludeNavigationPropertiesWithExpression<T>(navProps).Where<T>(expression).AsQueryable<T>()
                    : GetDbSetIncludeNavigationPropertiesWithExpression<T>(navProps).Where<T>(expression).AsNoTracking<T>().AsQueryable<T>();
            }
        }

        public virtual T FirstOrDefaultPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null, bool isTracking = false) where T : class
        {
            if (navProps == null)
            {
                return isTracking
                    ? Context.Set<T>().FirstOrDefault(expression)
                    : Context.Set<T>().AsNoTracking().FirstOrDefault(expression);
            }
            else
            {
                return isTracking
                    ? GetDbSetIncludeNavigationProperties<T>(navProps).FirstOrDefault(expression)
                    : GetDbSetIncludeNavigationProperties<T>(navProps).AsNoTracking().FirstOrDefault(expression);
            }
        }

        public virtual T FirstOrDefaultPure<T>(Expression<Func<T, bool>> expression, List<Expression<Func<T, object>>> navProps = null, bool isTracking = false) where T : class
        {
            if (navProps == null)
            {
                return isTracking
                    ? Context.Set<T>().FirstOrDefault(expression)
                    : Context.Set<T>().AsNoTracking().FirstOrDefault(expression);
            }
            else
            {
                return isTracking
                    ? GetDbSetIncludeNavigationPropertiesWithExpression<T>(navProps).FirstOrDefault(expression)
                    : GetDbSetIncludeNavigationPropertiesWithExpression<T>(navProps).AsNoTracking().FirstOrDefault(expression);
            }
        }

        public virtual IQueryable<T> FilterWithOrderBy<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order, bool isTracking = false) where T : class
        {
            return isTracking
                ? Context.Set<T>().Where<T>(expression).OrderBy<T, TResult>(order).AsQueryable<T>()
                : Context.Set<T>().Where<T>(expression).AsNoTracking<T>().OrderBy<T, TResult>(order).AsQueryable<T>();
        }

        public virtual IQueryable<T> FilterWithOrderByDescending<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order, bool isTracking = false) where T : class
        {
            return isTracking
                ? Context.Set<T>().Where<T>(expression).OrderByDescending<T, TResult>(order).AsQueryable<T>()
                : Context.Set<T>().Where<T>(expression).AsNoTracking<T>().OrderByDescending<T, TResult>(order).AsQueryable<T>();
        }


        public virtual IQueryable<T> Filter<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> orderBy, bool orderByASC, out int pageCount, int pageIndex, int pageSize, bool isTracking = false) where T : class
        {
            var dbSet = this.Context.Set<T>();
            int skipCount = (pageIndex - 1) * pageSize;
            IQueryable<T> resetSet = null;
            if (orderBy == null)
            {
                pageCount = 0;
                return null;
            }
            if (orderByASC)
            {
                resetSet = expression != null
                    ?
                    (isTracking
                      ? dbSet.Where<T>(expression).OrderBy(orderBy).AsQueryable<T>()
                      : dbSet.Where<T>(expression).AsNoTracking<T>().OrderBy(orderBy).AsQueryable<T>()
                    )
                    :
                    (isTracking
                      ? dbSet.OrderBy(orderBy).AsQueryable<T>()
                      : dbSet.AsNoTracking<T>().OrderBy(orderBy).AsQueryable<T>()
                    );
            }
            else
            {
                resetSet = expression != null
                    ?
                    (isTracking
                      ? dbSet.Where<T>(expression).OrderByDescending(orderBy).AsQueryable<T>()
                      : dbSet.Where<T>(expression).AsNoTracking<T>().OrderByDescending(orderBy).AsQueryable<T>()
                    )
                    :
                    (isTracking
                      ? dbSet.OrderByDescending(orderBy).AsQueryable<T>()
                      : dbSet.AsNoTracking<T>().OrderByDescending(orderBy).AsQueryable<T>()
                    );
            }
            var allCount = resetSet.Count();

            pageCount = allCount == 0 ? 1 : (allCount % pageSize == 0 ? (allCount / pageSize) : (allCount / pageSize) + 1);
            var page = skipCount == 0 ? resetSet.Take(pageSize) : resetSet.Skip(skipCount).Take(pageSize);
            return page;
        }

        public virtual IQueryable<T> Filter<T>(Expression<Func<T, bool>> expression, out int total, int index = 0, int size = 50, bool isTracking = false) where T : class
        {
            int skipCount = index * size;
            var resetSet = expression != null
                ?
                (isTracking
                  ? Context.Set<T>().Where<T>(expression).AsQueryable<T>()
                  : Context.Set<T>().Where<T>(expression).AsNoTracking<T>().AsQueryable<T>()
                )
                :
                (isTracking
                  ? Context.Set<T>().AsQueryable<T>()
                  : Context.Set<T>().AsNoTracking<T>().AsQueryable<T>()
                );
            resetSet = skipCount == 0 ? resetSet.Take(size) : resetSet.Skip(skipCount).Take(size);
            total = resetSet.Count();
            return resetSet.AsQueryable<T>();
        }
        #endregion

        public virtual void LoadNavigationProperty<T>(T TObject, string navigationProperty) where T : class
        {
            if (!Context.Entry<T>(TObject).Collection(navigationProperty).IsLoaded)
            {
                Context.Entry<T>(TObject).Collection(navigationProperty).Load();
            }
        }

        /// <summary>
        /// Retrieve DbSet with navigation properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private IQueryable<T> GetDbSetIncludeNavigationProperties<T>(List<string> navProps = null) where T : class
        {
            var query = Context.Set<T>().AsQueryable();
            if (navProps == null || navProps.Count == 0) return query;
            // query = navProps.Aggregate(query, (current, info) => current.Include(info));

            foreach (var navProp in navProps)
            {
                query = query.Include(navProp);
            }
            return query;
        }

        private IQueryable<T> GetDbSetIncludeNavigationPropertiesWithExpression<T>(List<Expression<Func<T, object>>> navProps = null) where T : class
        {
            var query = Context.Set<T>().AsQueryable();
            if (navProps == null || navProps.Count == 0) return query;
            query = navProps.Aggregate(query, (current, info) => current.Include(info));
            // foreach (var navProp in navProps)
            // {
            //     query = query.Include(navProp);
            // }
            return query;
        }
        #endregion

        #region Update
        public virtual int UpdateWithKey<T>(T TObject, string key = "Id", bool withSaveChange = true) where T : class
        {
            var entry = Context.Entry(TObject);

            var pkey = entry.Property(key).CurrentValue; //Context.Set<T>().DefaultIfEmpty().GetType().GetProperty(key).GetValue(TObject);

            if (entry.State == EntityState.Detached)
            {
                var set = Context.Set<T>();
                T attachedEntity = set.Find(pkey);  // access the key
                if (attachedEntity != null)
                {
                    var attachedEntry = Context.Entry(attachedEntity);
                    attachedEntry.CurrentValues.SetValues(TObject);
                }
                else
                {
                    entry.State = EntityState.Modified; // attach the entity
                }
            }
            if (withSaveChange)
            {
                return this.Commit();
            }
            return 0;
        }

        public virtual int UpdateRangeWithKey<T>(IEnumerable<T> TObjects, string key = "Id", bool withSaveChange = true) where T : class
        {
            var set = Context.Set<T>();
            //var keyProperty = set.DefaultIfEmpty().GetType().GetProperty(key); 
            var keyProperty = TObjects.FirstOrDefault().GetType().GetProperty(key);
            //For Test by L.Zhi
            //var TObject = TObjects.FirstOrDefault();
            //var getType = TObject.GetType();
            //var keyProperty = getType.GetProperty(key);
            foreach (var tObject in TObjects)
            {
                var entry = Context.Entry(tObject);
                var pkey = keyProperty.GetValue(tObject);
                if (entry.State == EntityState.Detached)
                {
                    T attachedEntity = set.Find(pkey);  // access the key
                    if (attachedEntity != null)
                    {
                        var attachedEntry = Context.Entry(attachedEntity);
                        attachedEntry.CurrentValues.SetValues(tObject);
                    }
                    else
                    {
                        entry.State = EntityState.Modified; // attach the entity
                    }
                }
            }
            if (withSaveChange)
            {
                return this.Commit();
            }
            return 0;
        }

        public virtual int Update<T>(T entity, bool withSaveChange = true) where T : class
        {
            Context.Set<T>().Update(entity);
            var result = SaveChangesInternal(withSaveChange, 1);
            return result;
        }

        public virtual int UpdateRange<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class
        {
            //Todo why not updateRange
            foreach (var entity in entities)
            {
                Context.Set<T>().Update(entity);
            }
            var result = SaveChangesInternal(withSaveChange, entities.Count());
            return result;
        }
        #endregion

        #region Delete
        public virtual T DeleteReturnEntity<T>(T entity, bool withSaveChange = true) where T : class
        {
            var entityResult = entity;
            Context.Set<T>().Remove(entity);
            SaveChangesInternal(withSaveChange, 1);
            return entityResult;
        }

        public virtual int Delete<T>(T entity, bool withSaveChange = true) where T : class
        {
            Context.Set<T>().Remove(entity);
            var result = SaveChangesInternal(withSaveChange, 1);
            return result;
        }

        public virtual IEnumerable<T> DeleteRangeReturnEntities<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class
        {
            var entityResult = entities;
            Context.Set<T>().RemoveRange(entities);
            SaveChangesInternal(withSaveChange, entities.Count());
            return entityResult;
        }

        public virtual int DeleteRange<T>(IEnumerable<T> entities, bool withSaveChange = true) where T : class
        {
            Context.Set<T>().RemoveRange(entities);
            var result = SaveChangesInternal(withSaveChange, entities.Count());
            return result;
        }

        public virtual int Delete<T>(Expression<Func<T, bool>> expression, bool withSaveChange = true) where T : class
        {
            var objects = Filter<T>(expression);
            foreach (var obj in objects)
            {
                Context.Set<T>().Remove(obj);
            }
            var result = SaveChangesInternal(withSaveChange, objects.Count());
            return result;
            //using (var ctx = Context.CreateLinqToDbConnection())
            //{
            //    var table = ctx.GetTable<T>();
            //    var affectCount = table.Where(predicate).Delete();
            //    return affectCount;
            //}
        }


        #endregion

        #region BySql

        public virtual IQueryable<T> FromSql<T>(string sql, params object[] param) where T : class
        {
            return Context.Set<T>().FromSqlRaw(sql, param);
        }

        public virtual IQueryable<T> FromSql<T>(string formattedSql) where T : class
        {
            return Context.Set<T>().FromSqlRaw(formattedSql);
        }

        public virtual List<T> SqlQuery<T>(string sql, params object[] param)
        {
            using (var command = Context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                if (param != null && param.Any())
                {
                    foreach (var p in param)
                    {
                        command.Parameters.Add(p);
                    }
                }
                Context.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    List<T> list = new List<T>();
                    while (result.Read())
                    {
                        var typeInfo = typeof(T);
                        if (typeInfo.Name == "String")
                        {
                            list.Add((T)result[0]);
                        }
                        else
                        {
                            var obj = Activator.CreateInstance<T>();
                            if (typeof(T).IsClass)
                            {
                                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                                {
                                    try
                                    {
                                        if (!Equals(result[prop.Name], DBNull.Value))
                                        {
                                            prop.SetValue(obj, result[prop.Name], null);
                                        }
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                obj = (T)result[0];
                            }

                            list.Add(obj);
                        }
                    }
                    return list;
                }
            }
        }
        public List<T> SqlQueryWithDBOpen<T>(string sql, params object[] param)
        {
            using (var command = Context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                if (param != null && param.Any())
                {
                    foreach (var p in param)
                    {
                        command.Parameters.Add(p);
                    }
                }
                using (var result = command.ExecuteReader())
                {
                    List<T> list = new List<T>();
                    while (result.Read())
                    {
                        var typeInfo = typeof(T);
                        if (typeInfo.Name == "String")
                        {
                            list.Add((T)result[0]);
                        }
                        else
                        {
                            var obj = Activator.CreateInstance<T>();
                            if (typeof(T).IsClass)
                            {
                                foreach (PropertyInfo prop in obj.GetType().GetProperties())
                                {

                                    if (!Equals(result[prop.Name], DBNull.Value))
                                    {
                                        prop.SetValue(obj, result[prop.Name], null);
                                    }
                                }
                            }
                            else
                            {
                                obj = (T)result[0];
                            }

                            list.Add(obj);
                        }
                    }
                    return list;
                }
            }
        }

        public int ExecuteSqlCommand(string sql, params object[] param)
        {
            return Context.Database.ExecuteSqlRaw(sql, param);
        }

        public virtual int ExecuteSqlCommand(string formattedSql)
        {
            return Context.Database.ExecuteSqlRaw(formattedSql);
        }

        public virtual int BatchExecuteSqlCommand(List<string> sqls)
        {
            string connectionStr = Context.Database.GetConnectionString();
            using (var con = new SqlConnection(connectionStr))
            {
                con.Open();
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandTimeout = 6000;
                    var sql = string.Join(";", sqls);
                    logger.Info($"BatchExecuteSqlCommand: {sql}");
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    return sqls.Count;
                }
            }
        }

        public int BatchExecuteSqlCommand(List<string> sqls, SqlParameter[] parameters)
        {
            string connectionStr = Context.Database.GetConnectionString();
            using (var con = new SqlConnection(connectionStr))
            {
                con.Open();
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandTimeout = 6000;
                    var sql = string.Join(";", sqls);
                    logger.Info(sql);
                    cmd.CommandText = sql;
                    if (!parameters.IsNullOrEmpty())
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    cmd.ExecuteNonQuery();
                    return sqls.Count;
                }
            }
        }

        #endregion

        #region Others 
        public virtual void EagerInitDatabase()
        {
            try
            {
                Type t = Context.GetType();
                var properties = t.GetProperties();
                var any = typeof(System.Linq.Queryable).GetMethods().Where(m => m.Name == "Any"
                                     && m.GetParameters().Length == 1
                                     && m.GetParameters()[0].ParameterType.IsGenericType).FirstOrDefault();
                foreach (var property in properties)
                {
                    Type pt = property.PropertyType;
                    if (pt.Name.Equals("IDbSet`1"))
                    {
                        var obj = property.GetValue(Context, null);
                        if (obj != null)
                        {
                            var result = any.MakeGenericMethod(pt.GetGenericArguments()[0]).Invoke(null, new object[] { obj });
                        }
                    }
                }
                logger.Debug("Database Context warm up finished.");
            }
            catch (Exception e)
            {
                logger.Error("Warm up db error:" + e.Message, e);
            }
        }

        public virtual void Execute(Action<DbContext> action)
        {
            action(Context);
        }

        public virtual void Attach<T>(T obj) where T : class
        {
            if (Context.Entry(obj).State == EntityState.Detached)
            {
                Context.Set<T>().Attach(obj);
            }
        }

        public virtual void DisableAutoDetectChanges()
        {
            Context.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public virtual void EnableAutoDetectChanges()
        {
            Context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        public virtual void DetectChanges()
        {
            Context.ChangeTracker.DetectChanges();
        }

        public virtual void ClearTracker()
        {
            Context.ChangeTracker.Clear();
        }
        public virtual void SetCommandTimeout(int? second)
        {
            Context.Database.SetCommandTimeout(second);
        }

        public virtual void EnableLazyLoad()
        {
            Context.ChangeTracker.LazyLoadingEnabled = true;
        }

        public virtual void DisableLazyLoad()
        {
            Context.ChangeTracker.LazyLoadingEnabled = false;
        }
        #endregion

        #region Commit  
        public virtual int Commit()
        {
            try
            {
                return Context.SaveChanges();
            }
            //catch (DbEntityValidationException ex)
            //{
            //    foreach (var validationError in ex.EntityValidationErrors)
            //    {
            //        var entity = validationError.Entry.Entity;
            //        logger.Warn($"Entity of type '{entity.GetType().Name}' in state '{validationError.Entry.State}' has the following validation errors:");
            //        foreach (var error in validationError.ValidationErrors)
            //        {
            //            logger.Warn($"Property: {error.PropertyName}, Error: {error.ErrorMessage}");
            //        }
            //        Context.Entry(entity).State = EntityState.Detached;
            //    }
            //    throw;
            //}
            //catch (DbUpdateException e)
            //{
            //    foreach (var entry in e.Entries)
            //    {
            //        logger.Warn($"Entity of type '{entry.Entity.GetType().Name}' in state '{entry.State}' commit failed.");
            //        entry.State = EntityState.Detached;
            //    }
            //    if ((e.InnerException is UpdateException) && (e.InnerException.InnerException is SqlException))
            //    {
            //        var sqlException = (SqlException)e.InnerException.InnerException;
            //        for (int i = 0; i < sqlException.Errors.Count; i++)
            //        {
            //            logger.Warn(sqlException.Errors[i].Message); ;
            //        }
            //    }
            //    throw;
            //}
            catch (DbUpdateException e)
            {
                foreach (var entry in e.Entries)
                {
                    logger.Warn($"Entity of type '{entry.Entity.GetType().Name}' in state '{entry.State}' commit failed.");
                    entry.State = EntityState.Detached;
                }
                if ((e.InnerException.InnerException is SqlException))
                {
                    var sqlException = (SqlException)e.InnerException.InnerException;
                    for (int i = 0; i < sqlException.Errors.Count; i++)
                    {
                        logger.Warn(sqlException.Errors[i].Message); ;
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public virtual int CommitHandleConcurrency()
        {
            try
            {
                bool saveFailed;
                int retryCount = 0;
                do
                {
                    saveFailed = false;
                    try
                    {
                        return Context.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        logger.Warn($"DbUpdateConcurrencyException has occurred, retryCount:{retryCount}");
                        saveFailed = true;
                        retryCount++;

                        try
                        {
                            // Update original values from the database
                            var entry = ex.Entries.Single();
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }
                        catch (Exception e)
                        {
                            logger.Error($"DbUpdateConcurrencyException resolve optimistic concurrency exceptions as client wins failed:{e.ToString()}");
                            throw;
                        }
                    }
                    Thread.Sleep(5 * 1000);
                }
                while (saveFailed && retryCount <= 3);
                logger.Error($"DbUpdateConcurrencyException failed, retryCount:{retryCount}, leave without change data.");
                return 0;
            }
            //catch (DbEntityValidationException ex)
            //{
            //    foreach (var validationError in ex.EntityValidationErrors)
            //    {
            //        var entity = validationError.Entry.Entity;
            //        logger.Warn($"Entity of type '{entity.GetType().Name}' in state '{validationError.Entry.State}' has the following validation errors:");
            //        foreach (var error in validationError.ValidationErrors)
            //        {
            //            logger.Warn($"Property: {error.PropertyName}, Error: {error.ErrorMessage}");
            //        }
            //        Context.Entry(entity).State = EntityState.Detached;
            //    }
            //    throw;
            //}
            //catch (DbUpdateException e)
            //{
            //    foreach (var entry in e.Entries)
            //    {
            //        logger.Warn($"Entity of type '{entry.Entity.GetType().Name}' in state '{entry.State}' commit failed.");
            //        entry.State = EntityState.Detached;
            //    }
            //    if ((e.InnerException is UpdateException) && (e.InnerException.InnerException is SqlException))
            //    {
            //        var sqlException = (SqlException)e.InnerException.InnerException;
            //        for (int i = 0; i < sqlException.Errors.Count; i++)
            //        {
            //            logger.Warn(sqlException.Errors[i].Message); ;
            //        }
            //    }
            //    throw;
            //}
            catch (DbUpdateException e)
            {
                foreach (var entry in e.Entries)
                {
                    logger.Warn($"Entity of type '{entry.Entity.GetType().Name}' in state '{entry.State}' commit failed.");
                    entry.State = EntityState.Detached;
                }
                if ((e.InnerException.InnerException is SqlException))
                {
                    var sqlException = (SqlException)e.InnerException.InnerException;
                    for (int i = 0; i < sqlException.Errors.Count; i++)
                    {
                        logger.Warn(sqlException.Errors[i].Message); ;
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private int SaveChangesInternal(bool saveChange, int affectCount)
        {
            if (saveChange)
                return this.SaveChanges(true);
            else
                return affectCount;
        }

        public virtual int SaveChanges(bool trackChange = true)
        {
            int applicationNumber;
            Context.ChangeTracker.AutoDetectChangesEnabled = trackChange;
            try
            {
                applicationNumber = Context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ex.Entries.Single().Reload();
                applicationNumber = Context.SaveChanges();
            }
            return applicationNumber;
        }

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        #endregion




        /// <summary>
        /// example:
        /// BulkUpdate<ImportUserModel, CUser>(sources,
        ///     (s, d) => s.Id == d.Id,
        ///     r => new CUser
        ///     {
        ///        Phone = r.Item1.Phone,
        ///        OfficePhone = r.Item1.OfficePhone,
        ///     });
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="joinCondition"></param>
        /// <param name="setter">Should return a new target object which contains the columns that need to be updated only. Item1 is source, Item2 is target</param>
  


        #region NoLock //Todo NoLock query
        public virtual T SingleWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return this.Context.Set<T>().FirstOrDefault(expression);
        }

        public virtual T FirstOrDefaultWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return this.Context.Set<T>().FirstOrDefault(expression);
        }

        public virtual List<T> AllWithNoLock<T>() where T : class
        {
            return this.All<T>().ToList();
        }

        public virtual bool AnyWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return Context.Set<T>().AsNoTracking().Any(expression);
        }

        public virtual bool ContainsWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return Context.Set<T>().AsNoTracking().Any<T>(expression);
        }

        public virtual T FindWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return this.Context.Set<T>().FirstOrDefault(expression);
        }

        public virtual T FindWithNoLockPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null) where T : class
        {
            if (navProps == null)
            {
                return Context.Set<T>().FirstOrDefault(expression);
            }
            else
            {
                return GetDbSetIncludeNavigationProperties<T>().FirstOrDefault(expression);
            }
        }

        public virtual T FindWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class
        {
            return this.Context.Set<T>().Where<T>(expression).OrderBy<T, TResult>(order).FirstOrDefault();
        }

        public virtual List<T> FilterWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return this.Filter(expression).ToList();
        }

        public virtual List<T> FilterWithNoTrackingWithNoLock<T>(Expression<Func<T, bool>> expression) where T : class
        {
            return this.FilterWithNoTracking(expression).ToList();
        }

        public virtual List<TResult> FilterWithNoLock<T, TResult>(Expression<Func<T, TResult>> expression) where T : class
        {
            return this.Filter(expression).ToList();
        }

        public virtual List<TResult> FilterWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> selector) where T : class
        {
            return this.Filter(expression, selector).ToList();
        }

        public virtual List<T> FilterWithNoLockPure<T>(Expression<Func<T, bool>> expression, List<string> navProps = null) where T : class
        {
            if (navProps == null)
            {
                return Context.Set<T>().Where<T>(expression).ToList();
            }
            else
            {
                return GetDbSetIncludeNavigationProperties<T>(navProps).Where<T>(expression).ToList();
            }
        }

        public virtual List<T> FilterWithNoLockPure<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class
        {
            return Context.Set<T>().Where<T>(expression).OrderBy(order).ToList();
        }

        public virtual List<T> FilterWithOrderByWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order) where T : class
        {
            return this.FilterWithOrderBy(expression, order).ToList();
        }

        public virtual List<T> FilterWithOrderByDescendingWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> order, bool isTracking = false) where T : class
        {
            return this.FilterWithOrderByDescending(expression, order, isTracking).ToList();
        }

        public virtual List<T> FilterWithNoLock<T, TResult>(Expression<Func<T, bool>> expression, Expression<Func<T, TResult>> orderBy, bool orderByASC, out int pageCount, int pageIndex, int pageSize, List<string> navProps = null) where T : class
        {
            var dbSet = this.Context.Set<T>();
            int skipCount = (pageIndex - 1) * pageSize;
            IQueryable<T> resetSet = null;
            if (orderBy == null)
            {
                pageCount = 0;
                return null;
            }
            if (orderByASC)
            {
                resetSet = expression != null ? dbSet.Where<T>(expression).OrderBy(orderBy).AsQueryable() : dbSet.OrderBy(orderBy).AsQueryable();
            }
            else
            {
                resetSet = expression != null ? dbSet.Where<T>(expression).OrderByDescending(orderBy).AsQueryable() : dbSet.OrderByDescending(orderBy).AsQueryable();
            }
            var allCount = resetSet.Count();
            pageCount = allCount == 0 ? 1 : (allCount % pageSize == 0 ? (allCount / pageSize) : (allCount / pageSize) + 1);
            var page = skipCount == 0 ? resetSet.Take(pageSize) : resetSet.Skip(skipCount).Take(pageSize);
            return page.ToList();
        }
        #endregion

        public void Dispose()
        {

        }
    }

    public class UpdateSpecification<T>
    {
        /// <summary>
        /// Set each column you want to update, Columns that belong to the primary key cannot be updated.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public UpdateSpecification<T> ColumnsToUpdate(params Expression<Func<T, object>>[] properties)
        {
            Properties = properties;
            return this;
        }

        public Expression<Func<T, object>>[] Properties { get; set; }
    }
}

