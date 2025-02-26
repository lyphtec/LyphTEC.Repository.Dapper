using System;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using LyphTEC.Repository.Dapper.Tests.Domain;
using LyphTEC.Repository.Dapper.Tests.Model;
using Xunit;

namespace LyphTEC.Repository.Dapper.Tests.SqlServer;

public class SqlServerRepositoryTests(SqlServerRepositoryFixture fixture) : IClassFixture<SqlServerRepositoryFixture>
{
    private readonly SqlServerRepositoryFixture _fixture = fixture;
    private readonly DapperRepository<Customer> _customerRepo = new(fixture.Settings);
    private readonly DapperRepository<Invoice> _invoiceRepo = new(fixture.Settings);

    void ClearCustomerRepo()
    {
        using var db = _fixture.CreateOpenDbConnection();
        db.Execute("truncate table [Customer];");
    }

    void ClearInvoiceRepo()
    {
        using var db = _fixture.CreateOpenDbConnection();
        db.Execute("truncate table [Invoice];");
    }

    [Fact]
    public void SaveInsert_Customer_Ok()
    {
        ClearCustomerRepo();

        var cust = Generator.Generate<Customer>();
        cust.Address = Generator.Generate<Address>();
        var dateAdded = DateTime.UtcNow;
        cust.Address.DateAdded = dateAdded;

        var saved = _customerRepo.Save(cust);

        Assert.Equal(1, saved.Id);
        Assert.Equal(cust.Address.City, saved.Address.City);
        Assert.Equal(dateAdded, saved.Address.DateAdded);
    }

    [Fact]
    public void SaveUpdate_Customer_Ok()
    {
        ClearCustomerRepo();

        var cust = _customerRepo.Save(Generator.Generate<Customer>());
        var before = cust.Email;

        cust.Email = "updated@me.com";

        var result = _customerRepo.Save(cust);

        Assert.NotEqual(before, result.Email);
    }

    [Fact]
    public void SaveInsert_Invoice_Ok()
    {
        ClearInvoiceRepo();

        var invDate = new DateTime(2016, 1, 1);
        var inv = Generator.Generate<Invoice>(x =>
            {
                x.InvoiceDate = invDate;
                x.BillingAddress = Generator.Generate<Address>();
            });

        var saved = _invoiceRepo.Save(inv);

        Assert.IsType<Guid>(saved.Id);
        Assert.Equal(inv.BillingAddress.City, saved.BillingAddress.City);
        Assert.Equal(invDate, saved.InvoiceDate);
    }

    [Fact]
    public void Get_ById_Ok()
    {
        ClearCustomerRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));
        var cust = Generator.Generate<Customer>(x => x.Email = "jdoe@acme.co");
        _customerRepo.Save(cust);

        var actual = _customerRepo.Get(4);

        Assert.NotNull(actual);
        Assert.Equal(cust.Email, actual.Email);
    }

    [Fact]
    public void Get_Linq_Ok()
    {
        ClearCustomerRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));
        var cust = Generator.Generate<Customer>(x => x.Email = "jdoe@acme.co");
        _customerRepo.Save(cust);

        var actual = _customerRepo.Get(x => x.Email.Equals(cust.Email));

        Assert.NotNull(actual);
        Assert.Equal(4, actual.Id);
    }

    [Fact]
    public void RemoveById_Ok()
    {
        ClearCustomerRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));
        var cust = Generator.Generate<Customer>(x => x.Email = "jdoe@acme.co");
        _customerRepo.Save(cust);

        var one = _customerRepo.Get(x => x.Email.Equals(cust.Email));

        _customerRepo.Remove(one.Id);

        Assert.Equal(3, _customerRepo.Count());

        var custLookup = _customerRepo.Get(one.Id);
        Assert.Null(custLookup);
    }

    [Fact]
    public void Remove_Ok()
    {
        ClearCustomerRepo();

        var cust = Generator.Generate<Customer>();
        _customerRepo.Save(cust);

        Assert.Equal(1, _customerRepo.Count());

        _customerRepo.Remove(cust);

        Assert.Equal(0, _customerRepo.Count());
    }

    [Fact]
    public void RemoveByIds_Ok()
    {
        ClearCustomerRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));

        var ids = _customerRepo.All().Take(2).Select(x => x.Id).ToList();

        _customerRepo.RemoveByIds(ids);

        Assert.Equal(1, _customerRepo.Count());
    }

    [Fact]
    public void RemoveAll_Ok()
    {
        ClearCustomerRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));

        Assert.Equal(3, _customerRepo.Count());
        
        _customerRepo.RemoveAll();

        Assert.Equal(0, _customerRepo.Count());
    }

    [Fact]
    public void Query_Linq_Ok()
    {
        ClearCustomerRepo();

        _customerRepo.Save(Generator.Generate<Customer>(5, x => x.Address = Generator.Generate<Address>()));
        _customerRepo.Save(Generator.Generate<Customer>(x => x.Company = "ACME"));

        Assert.Equal(6, _customerRepo.Count());

        var actual = _customerRepo.Query(x => x.Company.Equals("ACME"));

        Assert.Single(actual);
    }

    [Fact]
    public void CustomMapping_Save_Ok()
    {
        var repo = new DapperRepository<Customer>(_fixture.Settings, () =>
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof (CustomMapper<>);
        });

        ClearCustomerRepo();

        var cust = Generator.Generate<Customer>();
        cust.Address = Generator.Generate<Address>();

        var newCust = repo.Save(cust);

        Assert.Equal(1, repo.Count());
        Assert.NotNull(newCust);
    }

    [Import]
    public IRepository<Customer> MefCustomerRepo { get; set; }

    [Fact]
    public void MEF_Ok()
    {
        var config = new ContainerConfiguration()
            .WithAssembly(typeof (DapperRepository<>).Assembly)
            .WithExport(_fixture.Settings);

        using (var container = config.CreateContainer())
        {
            container.SatisfyImports(this);
        }

        ClearCustomerRepo();
        _customerRepo.Save(Generator.Generate<Customer>(5, x => x.Address = Generator.Generate<Address>()));

        var cust = Generator.Generate<Customer>(x =>
        {
            x.Company = "MEFFY";
            x.Address = Generator.Generate<Address>();
        });

        MefCustomerRepo.Save(cust);

        Assert.Equal(6, _customerRepo.Count());
    }

    [Fact]
    public async Task SaveAsync_Ok()
    {
        ClearCustomerRepo();

        var cust = Generator.Generate<Customer>();
        cust.Address = Generator.Generate<Address>();
        var dateAdded = DateTime.UtcNow;
        cust.Address.DateAdded = dateAdded;

        var saved = await _customerRepo.SaveAsync(cust);

        Assert.Equal(1, saved.Id);
        Assert.Equal(cust.Address.City, saved.Address.City);
        Assert.Equal(dateAdded, saved.Address.DateAdded);
    }
}
