using System.Reflection;
using Dapper;
using DapperExtensions;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LyphTEC.Repository.Dapper
{
    // Some resources that may be useful: 
    //  - http://stackoverflow.com/questions/15154783/pulling-apart-expressionfunct-object
    //  - http://stackoverflow.com/questions/16083895/grouping-lambda-expressions-by-operators-and-using-them-with-dapperextensions-p
    //  - http://blogs.msdn.com/b/mattwar/archive/2007/07/31/linq-building-an-iqueryable-provider-part-ii.aspx
    //  - http://msdn.microsoft.com/en-us/library/bb546136(v=vs.110).aspx
    //  - http://stackoverflow.com/questions/14437239/change-a-linq-expression-predicate-from-one-type-to-another/14439071#14439071

    /// <summary>
    /// <see cref="IRepository{TEntity}"/> implementation using Dapper as the persistence logic layer
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Export(typeof(IRepository<>))]
    [Shared]
    public class DapperRepository<TEntity> : IRepository<TEntity>, IRepositoryAsync<TEntity> where TEntity : class, IEntity
    {
        private readonly DbProviderFactory _factory;
        private readonly string _dbConnectionString;
        private readonly bool _isInitialised;

        /// <summary>
        /// Instantiates a new instance of <see cref="DapperRepository{TEntity}"/>
        /// </summary>
        /// <param name="settings">Connection settings</param>
        /// <param name="customInit">Provide your own initialisation instead of using the default <see cref="Init"/></param>
        [ImportingConstructor]
        public DapperRepository([Import]ConnectionStringSettings settings, [Import("CustomInit", AllowDefault = true)]Action customInit = null)
        {
            Contract.Requires<ArgumentNullException>(settings != null && !string.IsNullOrWhiteSpace(settings.ProviderName) && !string.IsNullOrWhiteSpace(settings.ConnectionString));

            _factory = DbProviderFactories.GetFactory(settings.ProviderName);
            _dbConnectionString = settings.ConnectionString;
            
            if (customInit == null)
                Init();
            else
                customInit();

            _isInitialised = true;
        }

        static DapperRepository()
        {
            DapperHelpers.ConfigureTypeHandlers();
        }

        private IDbConnection CreateDbConnection()
        {
            var db = _factory.CreateConnection();

            if (db == null)
                throw new Exception("Unable to create a new DbConnection");

            db.ConnectionString = _dbConnectionString;

            if (db.State != ConnectionState.Open)
                db.Open();

            return db;
        }

        void Init()
        {
            if (_isInitialised)
                return;

            //DapperExtensions.DapperExtensions.SetMappingAssemblies(new List<Assembly> { typeof(DapperRepository<>).Assembly });
            DapperExtensions.DapperExtensions.DefaultMapper = typeof (CustomClassMapper<>);
        }

        /// <summary>
        /// Specifies additional assemblies to find <see cref="IValueObject"/> to register as <see cref="SqlMapper.ITypeHandler"/> for Dapper <see cref="SqlMapper"/>
        /// </summary>
        /// <param name="assemblies">Assemblies to add</param>
        /// <remarks>By default, <see cref="Assembly.GetEntryAssembly"/> is already added when class is instantiated</remarks>
        public void SetValueObjectAssemblies(params Assembly[] assemblies)
        {
            DapperHelpers.AddValueObjectTypeHandlers(assemblies);
        }

        #region IRepository<TEntity> Members

        public IQueryable<TEntity> All(Expression<Func<TEntity, bool>> predicate = null)
        {
            var db = CreateDbConnection();
            
            var results = (predicate == null) ? db.GetList<TEntity>() : db.GetList<TEntity>(predicate.ToPredicateGroup());

            db.Close();
            db.Dispose();
            
            return results.AsQueryable();
        }

        public bool Any(Expression<Func<TEntity, bool>> predicate = null)
        {
            return Count(predicate) > 0;
        }

        public int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            var db = CreateDbConnection();

            var predicates = predicate.ToPredicateGroup();
            var result = db.Count<TEntity>(predicates);

            db.Close();
            db.Dispose();

            return result;
        }

        public TEntity One(Expression<Func<TEntity, bool>> predicate)
        {
            var db = CreateDbConnection();

            var result = All(predicate).SingleOrDefault();

            db.Close();
            db.Dispose();

            return result;
        }

        public TEntity One(object id)
        {
            var db = CreateDbConnection();

            var result = db.Get<TEntity>(id);

            db.Close();
            db.Dispose();

            return result;
        }

        public void Remove(object id)
        {
            var record = One(id);

            if (record == null) return;

            using (var db = CreateDbConnection())
            {
                db.Delete(record);

                db.Close();
            }
        }

        public void Remove(TEntity entity)
        {
            Remove(entity.Id);
        }

        public void RemoveAll()
        {
            using (var db = CreateDbConnection())
            {
                var t = typeof(TEntity);

                // TODO: This assumes that our table has the same name as TEntity
                db.Execute(string.Format("delete from {0}", t.Name));

                db.Close();
            }
        }

        public void RemoveByIds(System.Collections.IEnumerable ids)
        {
            if (ids == null)
                return;

            var predicate = Predicates.Field<TEntity>(x => x.Id, Operator.Eq, ids);

            using (var db = CreateDbConnection())
            {
                var success = db.Delete<TEntity>(predicate);

                db.Close();
            }
        }

        public TEntity Save(TEntity entity)
        {
            using (var db = CreateDbConnection())
            {
                // Insert
                if (entity.Id == null)
                    db.Insert(entity);
                else
                    db.Update(entity);

                db.Close();
                db.Dispose();
            }

            return entity;
        }

        public void SaveAll(IEnumerable<TEntity> entities)
        {
            // TODO: Consider using bulk insert : https://github.com/tmsmith/Dapper-Extensions/issues/18
            foreach (var entity in entities)
            {
                Save(entity);
            }
        }

        #endregion


        #region IRepositoryAsync<> Members

        public Task<IQueryable<TEntity>> AllAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> OneAsync(Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> OneAsync(object id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveAsync(object id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveByIdsAsync(System.Collections.IEnumerable ids)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveAllAsync(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> SaveAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
