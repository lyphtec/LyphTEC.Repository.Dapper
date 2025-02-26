using System;
using DapperExtensions.Mapper;
using LyphTEC.Repository.Dapper.Tests.Domain;

namespace LyphTEC.Repository.Dapper.Tests.SqlCe;

public class GuidEntity : Entity
{
    public string Name { get; set; }
    public DateTime DateField { get; set; }
    public Address Address { get; set; }
}

public class GuidEntityMapper : ClassMapper<GuidEntity>
{
    public GuidEntityMapper()
    {
        Map(x => x.Id).Key(KeyType.Guid);
        AutoMap();
    }
}
