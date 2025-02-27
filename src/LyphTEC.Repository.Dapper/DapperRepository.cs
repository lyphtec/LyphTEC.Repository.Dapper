using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;
using DapperExtensions.Predicate;
using LyphTEC.Repository.Dapper.Utils;
using IClassMapper = DapperExtensions.Mapper.IClassMapper;

namespace LyphTEC.Repository.Dapper;

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
public class DapperRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    private readonly DbProviderFactory _factory;
    private readonly string _dbConnectionString;
    private readonly bool _isInitialized;
    private static readonly ConcurrentDictionary<Type, List<(string ColumnNamne, string DataType)>> _schemas = new();

    /// <summary>
    /// Instantiates a new instance of <see cref="DapperRepository{TEntity}"/>
    /// </summary>
    /// <param name="settings">Connection settings</param>
    /// <param name="customInit">Provide your own initialisation instead of using the default <see cref="Init"/></param>
    [ImportingConstructor]
    public DapperRepository([Import]ConnectionStringSettings settings, [Import("CustomInit", AllowDefault = true)]Action customInit = null)
    {
        Contract.Requires<ArgumentNullException>(settings != null && !string.IsNullOrWhiteSpace(settings.ProviderName) && !string.IsNullOrWhiteSpace(settings.ConnectionString), $"{nameof(ConnectionStringSettings)} required");

#if NETSTANDARD2_1_OR_GREATER || NET462_OR_GREATER
        _factory = DbProviderFactories.GetFactory(settings.ProviderName);
#else
        _factory = DataAccessProvider.GetDbProviderFactory(settings.ProviderName);
#endif
        _dbConnectionString = settings.ConnectionString;
        
        if (customInit == null)
            Init();
        else
            customInit();

        SaveSchema();

        _isInitialized = true;
    }

    static DapperRepository()
    {
        DapperRepository.SetDefaultMappingAssembly();
    }

    private IDbConnection CreateDbConnection()
    {
        var db = _factory.CreateConnection() ?? throw new Exception("Unable to create a new DbConnection");
        db.ConnectionString = _dbConnectionString;

        if (db.State != ConnectionState.Open)
            db.Open();

        return db;
    }

    /// <summary>
    /// Default init. Will set <see cref="DefaultClassMapper{TEntity}"/> as the default mapper for DapperExtensions
    /// </summary>
    void Init()
    {
        if (_isInitialized)
            return;

        DapperExtensions.DapperExtensions.DefaultMapper = typeof (DefaultClassMapper<>);
    }

    void SaveSchema()
    {
        if (_isInitialized) return;

        var schema = _schemas.GetOrAdd(typeof(TEntity), []);

        using var db = CreateDbConnection();

        // TODO: This is currently specific to SQL Server - https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-schema-collections
        var dt = ((DbConnection)db).GetSchema("Columns", [null, null, typeof(TEntity).Name, null]);

        foreach (DataRow row in dt.Rows)
            schema.Add((row["COLUMN_NAME"].ToString(), row["DATA_TYPE"].ToString()));
    }

    /// <summary>
    /// Gets the DB schema for the table that <typeparamref name="TEntity"/> maps to
    /// </summary>
    public static IReadOnlyList<(string ColumnNamne, string DataType)> Schema
        => _schemas.GetOrAdd(typeof(TEntity), []);    

    #region IRepository<TEntity> Members

    public IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> predicate = null)
    {
        using var db = CreateDbConnection();
        
        var results = (predicate == null) ? db.GetList<TEntity>() : db.GetList<TEntity>(predicate.ToPredicateGroup());
        
        return results.AsQueryable();
    }

    public bool Any(Expression<Func<TEntity, bool>> predicate = null)
    {
        return Count(predicate) > 0;
    }

    public int Count(Expression<Func<TEntity, bool>> predicate = null)
    {
        using var db = CreateDbConnection();

        var predicates = predicate.ToPredicateGroup();
        var result = db.Count<TEntity>(predicates);

        return result;
    }

    public TEntity Get(Expression<Func<TEntity, bool>> predicate)
    {
        using var db = CreateDbConnection();

        var result = Query(predicate).FirstOrDefault();

        return result;
    }

    public TEntity Get(object id)
    {
        using var db = CreateDbConnection();

        // https://github.com/tmsmith/Dapper-Extensions/issues/315
        // var result = db.Get<TEntity>(id);

        var result = db.QuerySingleOrDefault<TEntity>($"select * from [{typeof(TEntity).Name}] where Id = @Id;", new { Id = id });
        return result;
    }

    public void Remove(object id)
    {
        using var db = CreateDbConnection();
        db.Execute($"delete from [{typeof(TEntity).Name}] where Id = @Id;", new { Id = id });
    }

    public void Remove(TEntity entity)
    {
        Remove(entity.Id);
    }

    public void RemoveAll()
    {
        using var db = CreateDbConnection();
        var t = typeof(TEntity);

        // TODO: This assumes that our table has the same name as TEntity
        db.Execute($"delete from [{t.Name}];");
    }

    public void RemoveByIds(System.Collections.IEnumerable ids)
    {
        if (ids == null)
            return;

        var predicate = Predicates.Field<TEntity>(x => x.Id, Operator.Eq, ids);

        using var db = CreateDbConnection();
        db.Delete<TEntity>(predicate);
    }

    public TEntity Save(TEntity entity)
    {
        using var db = CreateDbConnection();

        if (entity.Id == null)
        {
            SetGuidIdForInserts(entity);
            db.Insert(entity);
        }
        else
        {
            UpdateDateUpdated(entity);
            db.Update(entity, ignoreAllKeyProperties: true);
        }

        return entity;
    }

    public void Save(IEnumerable<TEntity> entities)
    {
        using var db = CreateDbConnection();

        // updates - db.Update(IEnumerable<TEntity>) is broken :(
        foreach (var entity in entities.Where(x => x.Id != null))
        {
            UpdateDateUpdated(entity);
            db.Update(entity, ignoreAllKeyProperties: true);
        }

        // inserts
        // TODO: Consider using bulk insert : https://github.com/tmsmith/Dapper-Extensions/issues/18
        var toInsert = entities.Where(x => x.Id == null).ToList();

        foreach (var entity in toInsert)
            SetGuidIdForInserts(entity);

        if (toInsert.Count > 0)
            db.Insert<TEntity>(toInsert);
    }

    #endregion


    #region IRepositoryAsync<> Members

    public async Task<IEnumerable<TEntity>> QueryAsync(Expression<Func<TEntity, bool>> predicate = null)
    {
        using var db = CreateDbConnection();
        
        var results = (predicate is null)
                            ? await db.GetListAsync<TEntity>()
                            : await db.GetListAsync<TEntity>(predicate.ToPredicateGroup());
        
        return results.AsQueryable();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null)
    {
        return await CountAsync(predicate) > 0;
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
    {
        using var db = CreateDbConnection();

        var predicates = predicate.ToPredicateGroup();
        var result = await db.CountAsync<TEntity>(predicates);

        return result;
    }

    public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return (await QueryAsync(predicate)).FirstOrDefault();
    }

    public async Task<TEntity> GetAsync(object id)
    {
        using var db = CreateDbConnection();
        var result = await db.QuerySingleOrDefaultAsync<TEntity>($"select * from [{typeof(TEntity).Name}] where Id = @Id;", new { Id = id });
        return result;
    }

    public async Task<bool> RemoveAllAsync()
    {
        using var db = CreateDbConnection();
        var t = typeof(TEntity);

        // TODO: This assumes that our table has the same name as TEntity
        await db.ExecuteAsync($"delete from [{t.Name}];");

        return true;
    }

    public async Task<bool> RemoveAsync(object id)
    {
        using var db = CreateDbConnection();
        var result = await db.ExecuteAsync($"delete from [{typeof(TEntity).Name}] where Id = @Id;", new { Id = id });
        return result > 0;
    }

    public Task<bool> RemoveAsync(TEntity entity)
    {
        return RemoveAsync(entity.Id);
    }

    public async Task<bool> RemoveByIdsAsync(System.Collections.IEnumerable ids)
    {
        if (ids == null)
            return false;

        var predicate = Predicates.Field<TEntity>(x => x.Id, Operator.Eq, ids);

        using var db = CreateDbConnection();
        return await db.DeleteAsync<TEntity>(predicate);
    }

    public async Task<bool> SaveAsync(IEnumerable<TEntity> entities)
    {
        using var db = CreateDbConnection();

        // updates
        foreach (var entity in entities.Where(x => x.Id != null))
        {
            UpdateDateUpdated(entity);
            await db.UpdateAsync(entity, ignoreAllKeyProperties: true);
        }

        // inserts
        var toInsert = entities.Where(x => x.Id == null).ToList();

        foreach (var entity in toInsert)
            SetGuidIdForInserts(entity);

        if (toInsert.Count > 0)
            db.Insert<TEntity>(toInsert);    // await db.InsertAsync(toInsert);    // <-- async insert seems to be broken

        return true;
    }

    public async Task<TEntity> SaveAsync(TEntity entity)
    {
        using var db = CreateDbConnection();

        if (entity.Id == null)
        {
            SetGuidIdForInserts(entity);
            db.Insert(entity);   //await db.InsertAsync(entity);    // <-- async insert seems to be broken
        }
        else
        {
            UpdateDateUpdated(entity);
            await db.UpdateAsync(entity, ignoreAllKeyProperties: true);
        }

        return entity;
    }

    #endregion

    #region Helpers

    // For tables where the primary key is a Guid, we need to set a new value for inserts
    static void SetGuidIdForInserts(TEntity entity)
    {
        var idCol = Schema.FirstOrDefault(x => x.ColumnNamne.Equals("Id", StringComparison.InvariantCultureIgnoreCase) && x.DataType.Equals("uniqueidentifier", StringComparison.InvariantCultureIgnoreCase));
        if (idCol == default) return;

        entity.Id = DapperRepository.GetNextGuid();
    }

    static void UpdateDateUpdated(TEntity obj)
    {
        if (obj is Entity entity)
            entity.DateUpdatedUtc = DateTime.UtcNow;
    }
    #endregion
}

/// <summary>
/// Provides access to global static members
/// </summary>
public static class DapperRepository
{
    private static readonly ConcurrentBag<Type> _sqlMapperTypes = [];

    /// <summary>
    /// Registers assemblies that contains <see cref="IValueObject"/> types as <see cref="SqlMapper.ITypeHandler"/> used by Dapper
    /// </summary>
    /// <param name="assemblies"></param>
    private static void AddValueObjectTypeHandlers(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            // Scan assembly for all types that are IValueObject & register
            // Assumes all these types have a default empty ctor
            var iValueObjectType = typeof (IValueObject);

            var ivoTypes = assembly
                .GetTypes()
                .Where(
                    t =>
                        t.IsInterface == false && t.IsAbstract == false &&
                        t.GetInterfaces().Contains(iValueObjectType));

            var handler = typeof (ValueObjectHandler<>);

            foreach (var ivoType in ivoTypes.Where(ivoType => !_sqlMapperTypes.Contains(ivoType)))
            {
                _sqlMapperTypes.Add(ivoType);

                var ctor = handler.MakeGenericType(ivoType).GetConstructor(Type.EmptyTypes);
                var instance = (SqlMapper.ITypeHandler) ctor.Invoke([]);
                SqlMapper.AddTypeHandler(ivoType, instance);
            }
        }
    }

    internal static void SetDefaultMappingAssembly()
    {
        // SqlMapper.AddTypeHandler(IdTypeHandler.Default);

        var ass = Assembly.GetEntryAssembly();

        if (ass != null)
            SetMappingAssemblies(ass);
    }

    /// <summary>
    /// Specifies additional assemblies that contains <see cref="IValueObject"/> types to register as <see cref="SqlMapper.ITypeHandler"/> for Dapper, and/or <see cref="IClassMapper"/> mapping types for DapperExtensions
    /// </summary>
    /// <param name="assemblies">Assemblies to add</param>
    /// <remarks>By default, <see cref="Assembly.GetEntryAssembly"/> is already added when class is instantiated</remarks>
    public static void SetMappingAssemblies(params Assembly[] assemblies)
    {
        AddValueObjectTypeHandlers(assemblies);

        DapperExtensions.DapperExtensions.SetMappingAssemblies(assemblies);
    }

    /// <summary>
    /// Returns the collection of types that have a <see cref="SqlMapper.ITypeHandler"/> registered
    /// </summary>
    public static IEnumerable<Type> SqlMapperTypes
    {
        get { return _sqlMapperTypes; }
    }

    /// <summary>
    /// Generates a COMB Guid which solves the fragmented index issue.
    /// See: http://davybrion.com/blog/2009/05/using-the-guidcomb-identifier-strategy
    /// </summary>
    /// <remarks>This is just a handy shortcut to <see cref="DapperExtensions.GetNextGuid"/></remarks>
    public static Guid GetNextGuid()
    {
        return DapperExtensions.DapperExtensions.GetNextGuid();
    }
}
