using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using Dapper;
using DapperExtensions.Sql;
using LyphTEC.Repository.Dapper.Tests.Model;
using ServiceStack.Text;
using Xunit;
using LyphTEC.Repository.Dapper.Tests.Extensions;

// ReSharper disable InconsistentNaming
namespace LyphTEC.Repository.Dapper.Tests
{
    public class DapperRepositoryTests : IUseFixture<DapperRepositoryFixture>
    {
        private DapperRepository<Customer> _repo;
        private IDbConnection _db;

        #region IUseFixture<CommonRepositoryFixture> Members

        public void SetFixture(DapperRepositoryFixture data)
        {
            _db = data.CreateDbConnection();

            data.ExecuteScript("CreateCustomerTable");

            _repo = new DapperRepository<Customer>(_db);
        }

        #endregion

        public void ClearRepo()
        {
            _db.Execute("delete from Customer");
        }

        private void DumpRepo()
        {
            _repo.All().PrintDump();
        }

        private static Customer NewCustomer(string firstName = "John", string lastName = "Smith", string email = "jsmith@acme.com", string company = "ACME")
        {
            var cust = new Customer
            {
                FirstName = firstName,
                LastName = lastName,
                Company = company,
                Email = email
            };

            return cust;
        }

        private static IEnumerable<Customer> NewCustomers()
        {
            var custs = new List<Customer>
                            {
                                NewCustomer(),
                                NewCustomer("Jane", "Doe", "jdoe@acme.com"),
                                NewCustomer("Jack", "Wilson", "jwilson@acme.com")
                            };

            return custs;
        }

        [Fact]
        public void Save_Ok()
        {
            ClearRepo();

            var cust = NewCustomer();

            var newCust = _repo.Save(cust);

            Assert.Equal(1, _repo.Count());
            Assert.NotNull(newCust);
            
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
            
        }

        [Import]
        public IRepository<Customer> MefCustomerRepo { get; set; }

        [Fact]
        public void MEF_Ok()
        {
            var db = new SqlCeConnection(_db.ConnectionString) as IDbConnection;

            var config = new ContainerConfiguration()
                .WithAssembly(typeof (DapperRepository<>).Assembly)
                .WithExport(db);

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