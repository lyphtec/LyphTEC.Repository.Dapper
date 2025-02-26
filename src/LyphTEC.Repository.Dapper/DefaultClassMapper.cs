using DapperExtensions.Mapper;

namespace LyphTEC.Repository.Dapper;

/// <summary>
/// Default <see cref="IClassMapper"/> implementation used by DapperExtensions when <see cref="DapperRepository{TEntity}"/>"/> is instantiated without a custom initiation action
/// </summary>
/// <typeparam name="TEntity">Class implementing <see cref="IEntity"/></typeparam>
public class DefaultClassMapper<TEntity> : ClassMapper<TEntity> where TEntity : class, IEntity
{
    public DefaultClassMapper()
    {
        var type = typeof (TEntity);
        Table(type.Name);

        Map(x => x.Id).Key(KeyType.Identity);
        
        AutoMap();
    }
}
