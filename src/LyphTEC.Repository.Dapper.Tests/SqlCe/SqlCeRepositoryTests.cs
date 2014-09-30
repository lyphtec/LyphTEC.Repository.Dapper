using System;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using Dapper;
using LyphTEC.Repository.Dapper.Tests.Extensions;
using LyphTEC.Repository.Dapper.Tests.Model;
using LyphTEC.Repository.Tests;
using LyphTEC.Repository.Tests.Domain;
using ServiceStack.Text;
using Xunit;

// ReSharper disable InconsistentNaming
namespace LyphTEC.Repository.Dapper.Tests.SqlCe
{
    public class SqlCeRepositoryTests : CommonRepositoryTest, IUseFixture<SqlCeRepositoryFixture>
    {
        private SqlCeRepositoryFixture _fixture;
        private DapperRepository<Customer> _customerRepo;
        private DapperRepository<GuidEntity> _geRepo;
        
        #region IUseFixture<CommonRepositoryFixture> Members

        public void SetFixture(SqlCeRepositoryFixture data)
        {
            _fixture = data;
            _customerRepo = new DapperRepository<Customer>(data.Settings);
            _geRepo = new DapperRepository<GuidEntity>(data.Settings);

            // since our test data entities live in a referenced assembly, we need to manually add it so the IValueObject can be found and registered as a type handler
            _customerRepo.SetValueObjectAssemblies(typeof(Address).Assembly);
            _geRepo.SetValueObjectAssemblies(typeof(Address).Assembly);

            CustomerRepo = _customerRepo;
        }

        #endregion

        public override void ClearRepo()
        {
            using (var db = _fixture.GetDbConnection())
            {
                db.Execute("delete from Customer");
                db.Execute("delete from GuidEntity");
            }
        }


        [Fact]
        public void Save_Ok()
        {
            ClearRepo();

            var cust = NewCustomer();
            cust.Address = NewAddress();
            cust.Address.DateAdded = new DateTime(2016, 1, 1);

            var newCust = _customerRepo.Save(cust);

            Assert.Equal(1, _customerRepo.Count());
            Assert.NotNull(newCust);
            Assert.Equal("Hidden Valley", newCust.Address.City);
            
            DumpRepo();
            newCust.PrintDump();
        }

        [Fact]
        public void Save_GuidEntity_Ok()
        {
            var entity = _fixture.NewGuidEntity();
            entity.Address= NewAddress(city: "Mod City");

            var result = _geRepo.Save(entity);

            Assert.NotNull(result.Id);
            Assert.IsType<Guid>(result.Id);
            Assert.Equal("Mod City", result.Address.City);
            
            result.PrintDump();
        }

        [Fact]
        public void Save_Update_Ok()
        {
            ClearRepo();

            var cust = _customerRepo.Save(NewCustomer());

            var before = cust.Email;

            cust.Email = "updated@me.com";
            
            var result = _customerRepo.Save(cust);

            Assert.NotEqual(before, result.Email);

            DumpRepo();
        }

        [Fact]
        public void One_Linq_Ok()
        {
            ClearRepo();

            _customerRepo.SaveAll(NewCustomers());

            var actual = _customerRepo.One(x => x.Email.Equals("jsmith@acme.com"));

            Assert.NotNull(actual);

            actual.PrintDump();

            DumpRepo();
        }

        [Fact]
        public void RemoveById_Ok()
        {
            ClearRepo();

            _customerRepo.SaveAll(NewCustomers());

            var one = _customerRepo.One(x => x.Email.Equals("jsmith@acme.com"));

            Console.WriteLine("Removing Id: {0}", one.Id);
            _customerRepo.Remove(one.Id);

            Assert.Equal(2, _customerRepo.Count());

            var cust = _customerRepo.One(one.Id);
            Assert.Null(cust);

            DumpRepo();
        }

        [Fact]
        public void Remove_Ok()
        {
            ClearRepo();

            var cust = NewCustomer();
            _customerRepo.Save(cust);

            Assert.Equal(1, _customerRepo.Count());

            _customerRepo.Remove(cust);

            Assert.Equal(0, _customerRepo.Count());
        }

        [Fact]
        public void RemoveByIds_Ok()
        {
            ClearRepo();

            _customerRepo.SaveAll(NewCustomers());

            var ids = _customerRepo.All().Take(2).Select(x => x.Id).ToList();

            Console.WriteLine("Removing Ids: ");

            ids.PrintDump();

            _customerRepo.RemoveByIds(ids);

            Assert.Equal(1, _customerRepo.Count());

            DumpRepo();
        }

        [Fact]
        public void RemoveAll_Ok()
        {
            ClearRepo();

            _customerRepo.SaveAll(NewCustomers());

            Assert.Equal(3, _customerRepo.Count());
            
            _customerRepo.RemoveAll();

            Assert.Equal(0, _customerRepo.Count());
        }

        [Fact]
        public void All_Linq_Ok()
        {
            ClearRepo();

            _customerRepo.SaveAll(NewCustomers());
            _customerRepo.Save(NewCustomer("James", "Harrison", "jharrison@foobar.com", "FooBar"));

            Assert.Equal(4, _customerRepo.Count());

            DumpRepo();

            var actual = _customerRepo.All(x => x.Company.Equals("ACME"));

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
            _customerRepo.SaveAll(NewCustomers());

            var cust = NewCustomer("MEF", "Head", "mef@meffy.com", "MEFFY");

            MefCustomerRepo.Save(cust);

            Assert.Equal(4, _customerRepo.Count());

            DumpRepo();
        }
    }
}
// ReSharper enable InconsistentNaming