using Dapper;
using LyphTEC.Repository.Dapper.Tests.Extensions;
using LyphTEC.Repository.Dapper.Tests.Model;
using LyphTEC.Repository.Tests;
using LyphTEC.Repository.Tests.Domain;
using ServiceStack.Text;
using System;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using Xunit;

// ReSharper disable InconsistentNaming
namespace LyphTEC.Repository.Dapper.Tests
{
    public class DapperRepositoryTests : CommonRepositoryTest, IUseFixture<DapperRepositoryFixture>
    {
        private DapperRepositoryFixture _fixture;
        private DapperRepository<Customer> _repo;
        
        #region IUseFixture<CommonRepositoryFixture> Members

        public void SetFixture(DapperRepositoryFixture data)
        {
            _fixture = data;
            _repo = new DapperRepository<Customer>(data.Settings);

            // since our test data entities live in a referenced assembly, we need to manually add it so the IValueObject can be found and registered as a type handler
            _repo.SetValueObjectAssemblies(typeof(Address).Assembly);

            CustomerRepo = _repo;
        }

        #endregion

        public override void ClearRepo()
        {
            using (var db = _fixture.GetDbConnection())
            {
                db.Execute("delete from Customer");
            }
        }


        [Fact]
        public void Save_Ok()
        {
            ClearRepo();

            var cust = NewCustomer();
            cust.Address = NewAddress();

            var newCust = _repo.Save(cust);

            Assert.Equal(1, _repo.Count());
            Assert.NotNull(newCust);
            Assert.Equal("Hidden Valley", newCust.Address.City);
            
            DumpRepo();
            newCust.PrintDump();
        }

        [Fact]
        public void Save_Update_Ok()
        {
            ClearRepo();

            var cust = _repo.Save(NewCustomer());

            var before = cust.Email;

            cust.Email = "updated@me.com";
            
            var result = _repo.Save(cust);

            Assert.NotEqual(before, result.Email);

            DumpRepo();
        }

        [Fact]
        public void One_Linq_Ok()
        {
            ClearRepo();

            _repo.SaveAll(NewCustomers());

            var actual = _repo.One(x => x.Email.Equals("jsmith@acme.com"));

            Assert.NotNull(actual);

            actual.PrintDump();

            DumpRepo();
        }

        [Fact]
        public void RemoveById_Ok()
        {
            ClearRepo();

            _repo.SaveAll(NewCustomers());

            var one = _repo.One(x => x.Email.Equals("jsmith@acme.com"));

            Console.WriteLine("Removing Id: {0}", one.Id);
            _repo.Remove(one.Id);

            Assert.Equal(2, _repo.Count());

            var cust = _repo.One(one.Id);
            Assert.Null(cust);

            DumpRepo();
        }

        [Fact]
        public void Remove_Ok()
        {
            ClearRepo();

            var cust = NewCustomer();
            _repo.Save(cust);

            Assert.Equal(1, _repo.Count());

            _repo.Remove(cust);

            Assert.Equal(0, _repo.Count());
        }

        [Fact]
        public void RemoveByIds_Ok()
        {
            ClearRepo();

            _repo.SaveAll(NewCustomers());

            var ids = _repo.All().Take(2).Select(x => x.Id).ToList();

            Console.WriteLine("Removing Ids: ");

            ids.PrintDump();

            _repo.RemoveByIds(ids);

            Assert.Equal(1, _repo.Count());

            DumpRepo();
        }

        [Fact]
        public void RemoveAll_Ok()
        {
            ClearRepo();

            _repo.SaveAll(NewCustomers());

            Assert.Equal(3, _repo.Count());
            
            _repo.RemoveAll();

            Assert.Equal(0, _repo.Count());
        }

        [Fact]
        public void All_Linq_Ok()
        {
            ClearRepo();

            _repo.SaveAll(NewCustomers());
            _repo.Save(NewCustomer("James", "Harrison", "jharrison@foobar.com", "FooBar"));

            Assert.Equal(4, _repo.Count());

            DumpRepo();

            var actual = _repo.All(x => x.Company.Equals("ACME"));

            Assert.Equal(3, actual.Count());

            Console.WriteLine("After filter: Company == 'ACME'");
            actual.PrintDump();
        }

        [Fact]
        public void CustomMapping_Save_Ok()
        {
            var repo = new DapperRepository<Customer>(_fixture.Settings, () =>
            {
                DapperExtensions.DapperExtensions.DefaultMapper = typeof (CustomMapper<>);
            });

            ClearRepo();

            var cust = NewCustomer();
            cust.Address = NewAddress();

            var newCust = repo.Save(cust);

            Assert.Equal(1, repo.Count());
            Assert.NotNull(newCust);
            
            DumpRepo();
            newCust.PrintDump();
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
            _repo.SaveAll(NewCustomers());

            var cust = NewCustomer("MEF", "Head", "mef@meffy.com", "MEFFY");

            MefCustomerRepo.Save(cust);

            Assert.Equal(4, _repo.Count());

            DumpRepo();
        }
    }
}
// ReSharper enable InconsistentNaming