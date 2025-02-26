using System;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using Dapper;
using LyphTEC.Repository.Dapper.Tests.Domain;
using LyphTEC.Repository.Dapper.Tests.Model;
using Xunit;

namespace LyphTEC.Repository.Dapper.Tests.SqlCe;

public class SqlCeRepositoryTests(SqlCeRepositoryFixture fixture) : IClassFixture<SqlCeRepositoryFixture>
{
    private readonly SqlCeRepositoryFixture _fixture = fixture;
    private readonly DapperRepository<Customer> _customerRepo = new(fixture.Settings);
    private readonly DapperRepository<GuidEntity> _geRepo = new(fixture.Settings);

    private void ClearRepo()
    {
        using var db = _fixture.GetDbConnection();
        db.Execute("delete from Customer");
        db.Execute("delete from GuidEntity");
    }

    [Fact]
    public void Save_Ok()
    {
        ClearRepo();

        var cust = Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>());
        cust.Address.DateAdded = new DateTime(2016, 1, 1);

        var newCust = _customerRepo.Save(cust);

        Assert.Equal(1, _customerRepo.Count());
        Assert.NotNull(newCust);
        Assert.Equal(cust.Address.City, newCust.Address.City);
    }

    [Fact]
    public void Save_GuidEntity_Ok()
    {
        var entity = SqlCeRepositoryFixture.NewGuidEntity();
        entity.Address= Generator.Generate<Address>();

        var result = _geRepo.Save(entity);

        Assert.NotNull(result.Id);
        Assert.IsType<Guid>(result.Id);
        Assert.Equal(entity.Address.City, result.Address.City);
    }

    [Fact]
    public void Save_Update_Ok()
    {
        ClearRepo();

        var cust = _customerRepo.Save(Generator.Generate<Customer>(x => x.Address = Generator.Generate<Address>()));

        var before = cust.Email;

        cust.Email = "updated@me.com";
        
        var result = _customerRepo.Save(cust);

        Assert.NotEqual(before, result.Email);
    }

    [Fact]
    public void Get_Linq_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Address = Generator.Generate<Address>()));
        var cust = Generator.Generate<Customer>(x => x.Email = "jsmith@acme.co");
        _customerRepo.Save(cust);

        var actual = _customerRepo.Get(x => x.Email.Equals(cust.Email));

        Assert.NotNull(actual);
    }

    [Fact]
    public void RemoveById_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Address = Generator.Generate<Address>()));
        var cust = Generator.Generate<Customer>(x => x.Email = "jsmith@acme.co");
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
        ClearRepo();

        var cust = Generator.Generate<Customer>();
        _customerRepo.Save(cust);

        Assert.Equal(1, _customerRepo.Count());

        _customerRepo.Remove(cust);

        Assert.Equal(0, _customerRepo.Count());
    }

    [Fact]
    public void RemoveByIds_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Address = Generator.Generate<Address>()));

        var ids = _customerRepo.All().Take(2).Select(x => x.Id).ToList();

        _customerRepo.RemoveByIds(ids);

        Assert.Equal(1, _customerRepo.Count());
    }

    [Fact]
    public void RemoveAll_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Address = Generator.Generate<Address>()));

        Assert.Equal(3, _customerRepo.Count());
        
        _customerRepo.RemoveAll();

        Assert.Equal(0, _customerRepo.Count());
    }

    [Fact]
    public void Query_Linq_Ok()
    {
        ClearRepo();

        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Company = "ACME"));
        var cust = Generator.Generate<Customer>();
        _customerRepo.Save(cust);

        Assert.Equal(4, _customerRepo.Count());

        var actual = _customerRepo.Query(x => x.Company.Equals("ACME"));

        Assert.Equal(3, actual.Count());
    }

    [Fact]
    public void CustomMapping_Save_Ok()
    {
        var repo = new DapperRepository<Customer>(_fixture.Settings, () =>
        {
            DapperExtensions.DapperExtensions.DefaultMapper = typeof (CustomMapper<>);
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
            .WithAssembly(typeof (DapperRepository<>).Assembly)
            .WithExport(_fixture.Settings);

        using (var container = config.CreateContainer())
        {
            container.SatisfyImports(this);
        }

        ClearRepo();
        _customerRepo.Save(Generator.Generate<Customer>(3, x => x.Address = Generator.Generate<Address>()));

        var cust = Generator.Generate<Customer>(x => x.Company = "ACME");

        MefCustomerRepo.Save(cust);

        Assert.Equal(4, _customerRepo.Count());
    }
}