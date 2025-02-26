using DapperExtensions.Mapper;

namespace LyphTEC.Repository.Dapper.Tests.Model;

public class CustomMapper<TEntity> : ClassMapper<TEntity> where TEntity : class, IEntity
{
    public CustomMapper()
    {
        var type = typeof (TEntity);
        Table(type.Name);
        Map(x => x.Id).Key(KeyType.Identity);
        AutoMap();
    }
}