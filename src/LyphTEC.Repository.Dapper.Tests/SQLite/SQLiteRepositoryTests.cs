using System;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using LyphTEC.Repository.Dapper.Tests.Domain;
using LyphTEC.Repository.Dapper.Tests.Model;
using Xunit;

namespace LyphTEC.Repository.Dapper.Tests.SQLite;

public class SQLiteRepositoryTests(SQLiteRepositoryFixture fixture) : IClassFixture<SQLiteRepositoryFixture>
{
    private readonly SQLiteRepositoryFixture _fixture = fixture;
    private readonly DapperRepository<Customer> _customerRepo = new(fixture.Settings);
    private readonly DapperRepository<Invoice> _invoiceRepo = new(fixture.Settings);

    private void ClearRepo()
    {
        using var db = _fixture.CreateOpenDbConnection();
        db.Execute("delete from [Customer]; delete from [Invoice];");
    }

    [Fact]
    public void SaveInsert_Customer_Ok()
    {
        ClearRepo();

        var cust = Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>());
        var dateAdded = new DateTime(2016, 1, 1);
        cust.Address.DateAdded = dateAdded;

        var saved = _customerRepo.Save(cust);

        Assert.Equal(1, saved.Id);
        Assert.Equal(cust.Address.City, saved.Address.City);
        Assert.Equal(dateAdded, saved.Address.DateAdded);
    }

    [Fact]
    public void SaveUpdate_Customer_Ok()
    {
        ClearRepo();

        var cust = _customerRepo.Save(Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>()));
        var before = cust.Email;

        cust.Email = "updated@me.com";

        var result = _customerRepo.Save(cust);

        Assert.NotEqual(before, result.Email);
    }

    [Fact]
    public void SaveInsert_Invoice_Ok()
    {
        ClearRepo();

        var invDate = new DateTime(2016, 1, 1);
        var inv = Generator.Generate<Invoice>(x => x.BillingAddress = Generator.Generate<Address>());

        var saved = _invoiceRepo.Save(inv);

        Assert.IsType<Guid>(saved.Id);
        Assert.Equal(inv.BillingAddress.City, saved.BillingAddress.City);
        Assert.Equal(invDate, saved.InvoiceDate);
    }

    [Fact]
    public void Get_ById_Ok()
    {
        ClearRepo();

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
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));
        var cust = Generator.Generate<Customer>(x => x.Company = "ACME");
        _customerRepo.Save(cust);

        var actual = _customerRepo.Get(x => x.Company.Equals(cust.Company));

        Assert.NotNull(actual);
        Assert.Equal(4, actual.Id);
    }

    [Fact]
    public void RemoveById_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));
        var cust = Generator.Generate<Customer>(x => x.Company = "ACME");
        _customerRepo.Save(cust);

        var one = _customerRepo.Get(x => x.Company.Equals(cust.Company));

        _customerRepo.Remove(one.Id);

        Assert.Equal(3, _customerRepo.Count());

        var custLookup = _customerRepo.Get(one.Id);
        Assert.Null(custLookup);
    }

    [Fact]
    public void Remove_Ok()
    {
        ClearRepo();

        var cust = Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>());
        _customerRepo.Save(cust);

        Assert.Equal(1, _customerRepo.Count());

        _customerRepo.Remove(cust);

        Assert.Equal(0, _customerRepo.Count());
    }

    [Fact]
    public void RemoveByIds_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));

        var ids = _customerRepo.All().Take(2).Select(x => x.Id).ToList();

        _customerRepo.RemoveByIds(ids);

        Assert.Equal(1, _customerRepo.Count());
    }

    [Fact]
    public void RemoveAll_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3));

        Assert.Equal(3, _customerRepo.Count());

        _customerRepo.RemoveAll();

        Assert.Equal(0, _customerRepo.Count());
    }

    [Fact]
    public void Query_Linq_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Company = "ACME"));
        _customerRepo.Save(Generator.Generate<Customer>());

        Assert.Equal(4, _customerRepo.Count());

        var actual = _customerRepo.Query(x => x.Company.Equals("ACME"));

        Assert.Equal(3, actual.Count());
    }

    [Fact]
    public void CustomMapping_Save_Ok()
    {
        var repo = new DapperRepository<Customer>(_fixture.Settings, () =>
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof(CustomMapper<>);
        });

        ClearRepo();

        var cust = Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>());

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
            .WithAssembly(typeof(DapperRepository<>).Assembly)
            .WithExport(_fixture.Settings);

        using (var container = config.CreateContainer())
        {
            container.SatisfyImports(this);
        }

        ClearRepo();
        _customerRepo.Save(Generator.Generate<Customer>(3));

        var cust = Generator.Generate<Customer>(x => x.Company = "MEFFY");

        MefCustomerRepo.Save(cust);

        Assert.Equal(4, _customerRepo.Count());
    }

    [Fact]
    public async Task SaveAsync_Ok()
    {
        ClearRepo();

        var cust = Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>());
        var dateAdded = new DateTime(2016, 1, 1);
        cust.Address.DateAdded = dateAdded;

        var saved = await _customerRepo.SaveAsync(cust);

        Assert.Equal(1, saved.Id);
        Assert.Equal(cust.Address.City, saved.Address.City);
        Assert.Equal(dateAdded, saved.Address.DateAdded);
    }
}
