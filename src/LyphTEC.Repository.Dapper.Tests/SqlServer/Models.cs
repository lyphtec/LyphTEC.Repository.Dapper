using DapperExtensions.Mapper;
using LyphTEC.Repository.Tests.Domain;

namespace LyphTEC.Repository.Dapper.Tests.SqlServer
{
    public class InvoiceMapper : ClassMapper<Invoice>
    {
        public InvoiceMapper()
        {
            Table("Invoice");
            Map(x => x.Id).Key(KeyType.Guid);
            AutoMap();
        }
    }
}
