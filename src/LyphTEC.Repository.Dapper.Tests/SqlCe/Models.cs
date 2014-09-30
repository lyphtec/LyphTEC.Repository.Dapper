using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DapperExtensions.Mapper;
using LyphTEC.Repository.Tests.Domain;

namespace LyphTEC.Repository.Dapper.Tests.SqlCe
{
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
            Table("GuidEntity");
            Map(x => x.Id).Key(KeyType.Guid);
            AutoMap();
        }
    }
}
