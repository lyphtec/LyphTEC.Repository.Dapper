using System;
using System.Collections.Generic;
using System.Composition;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;

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
    public class DapperRepository<TEntity> : IRepository<TEntity>, IDisposable
        where TEntity : class, IEntity
    {
        private readonly IDbConnection _db;
        private static bool _isInitialised;

        /// <summary>
        /// Instantiates a new instance of <see cref="DapperRepository{TEntity}"/>
        /// </summary>
        /// <param name="db">IDbConnection to use</param>
        /// <param name="customInit">Provide your own initialisation instead of using the default <see cref="Init"/></param>
        [ImportingConstructor]
        public DapperRepository([Import]IDbConnection db, [Import("CustomInit", AllowDefault = true)]Action customInit = null)
        {
            Contract.Requires<ArgumentNullException>(db != null);

            _db = db;

            if (customInit == null)
                Init();
            else
                customInit();

            _isInitialised = true;
        }

        private IDbConnection GetOpenConnection()
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            return _db;
        }

        static void Init()
        {
            if (_isInitialised)
                return;

            //DapperExtensions.DapperExtensions.SetMappingAssemblies(new List<Assembly> { typeof(DapperRepository<>).Assembly });
            DapperExtensions.DapperExtensions.DefaultMapper = typeof (CustomClassMapper<>);
        }

        #region IRepository<TEntity> Members

        public IQueryable<TEntity> All(Expression<Func<TEntity, bool>> predicate = null)
        {
            var db = GetOpenConnection();

            var results = (predicate == null) ? db.GetList<TEntity>() : db.GetList<TEntity>(predicate.ToPredicateGroup());

            db.Close();

            return results.AsQueryable();
        }

        public bool Any(Expression<Func<TEntity, bool>> predicate = null)
        {
            return Count(predicate) > 0;
        }

        public int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            var db = GetOpenConnection();

            var predicates = predicate.ToPredicateGroup();
            var result = db.Count<TEntity>(predicates);

            db.Close();

            return result;
        }

        public TEntity One(Expression<Func<TEntity, bool>> predicate)
        {
            var db = GetOpenConnection();

            var result = All(predicate).SingleOrDefault();

            db.Close();

            return result;
        }

        public TEntity One(object id)
        {
            var db = GetOpenConnection();

            var result = db.Get<TEntity>(id);

            db.Close();

            return result;
        }

        public void Remove(object id)
        {
            var record = One(id);

            if (record == null) return;

            var db = GetOpenConnection();

            db.Delete(record);

            db.Close();
        }

        public void Remove(TEntity entity)
        {
            Remove(entity.Id);
        }

        public void RemoveAll()
        {
            var db = GetOpenConnection();

            var t = typeof(TEntity);

            // TODO: This assumes that our table has the same name as TEntity
            db.Execute(string.Format("delete from {0}", t.Name));

            db.Close();
        }

        public void RemoveByIds(System.Collections.IEnumerable ids)
        {
            if (ids == null)
                return;

            var predicate = Predicates.Field<TEntity>(x => x.Id, Operator.Eq, ids);
            var db = GetOpenConnection();

            var success = db.Delete<TEntity>(predicate);

            db.Close();
        }

        public TEntity Save(TEntity entity)
        {
            var db = GetOpenConnection();

            // Insert
            if (entity.Id == null)
            {
                db.Insert(entity);
            }
            else
            {
                var dbRecord = db.Get<TEntity>((object)entity.Id);

                if (dbRecord == null)
                    throw new ArgumentException("Entity does not exist in Db. Cannot update.", "entity");

                db.Update(entity);
            }

            db.Close();

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


        #region IDisposable Members

        public void Dispose()
        {
            if (_db == null) return;

            if (_db.State != ConnectionState.Closed)
                _db.Close();

            _db.Dispose();
        }

        #endregion
    }
}
