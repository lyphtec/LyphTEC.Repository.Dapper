using DapperExtensions.Mapper;
using LyphTEC.Repository.Dapper.Tests.Domain;

namespace LyphTEC.Repository.Dapper.Tests.SqlServer;

public class InvoiceMapper : ClassMapper<Invoice>
{
    public InvoiceMapper()
    {
        Map(x => x.Id).Key(KeyType.Guid);
        AutoMap();
    }
}
