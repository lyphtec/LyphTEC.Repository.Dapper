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

namespace LyphTEC.Repository.Dapper.Tests.SqlServer
{
    public class SqlServerRepositoryTests : CommonRepositoryTest, IUseFixture<SqlServerRepositoryFixture>
    {
        private SqlServerRepositoryFixture _fixture;
        private DapperRepository<Customer> _customerRepo;
        private DapperRepository<Invoice> _invoiceRepo;

        public void SetFixture(SqlServerRepositoryFixture data)
        {
            _fixture = data;

            _customerRepo = new DapperRepository<Customer>(data.Settings);
            _invoiceRepo = new DapperRepository<Invoice>(data.Settings);

            CustomerRepo = _customerRepo;
        }

        void ClearCustomerRepo()
        {
            using (var db = _fixture.CreateOpenDbConnection())
            {
                db.Execute("truncate table [Customer];");
            }
        }

        void ClearInvoiceRepo()
        {
            using (var db = _fixture.CreateOpenDbConnection())
            {
                db.Execute("truncate table [Invoice];");
            }
        }

        public override void ClearRepo()
        {
            ClearCustomerRepo();
            ClearInvoiceRepo();
        }

        [Fact]
        public void SaveInsert_Customer_Ok()
        {
            ClearCustomerRepo();

            var cust = NewCustomer();
            cust.Address = NewAddress();
            var dateAdded = new DateTime(2016, 1, 1);
            cust.Address.DateAdded = dateAdded;

            var saved = _customerRepo.Save(cust);

            Assert.Equal(1, saved.Id);
            Assert.Equal("Hidden Valley", saved.Address.City);
            Assert.Equal(dateAdded, saved.Address.DateAdded);

            saved.PrintDump();
        }

        [Fact]
        public void SaveUpdate_Customer_Ok()
        {
            ClearCustomerRepo();

            var cust = _customerRepo.Save(NewCustomer());
            var before = cust.Email;

            cust.Email = "updated@me.com";

            var result = _customerRepo.Save(cust);

            Assert.NotEqual(before, result.Email);

            result.PrintDump();
        }

        [Fact]
        public void SaveInsert_Invoice_Ok()
        {
            ClearInvoiceRepo();

            var invDate = new DateTime(2016, 1, 1);
            var inv = new Invoice
            {
                CustomerId = 1,
                BillingAddress = NewAddress(),
                InvoiceDate = invDate,
                Total = 34.56M
            };

            var saved = _invoiceRepo.Save(inv);

            Assert.IsType<Guid>(saved.Id);
            Assert.Equal("Hidden Valley", saved.BillingAddress.City);
            Assert.Equal(invDate, saved.InvoiceDate);


            saved.PrintDump();
        }

        [Fact]
        public void One_Linq_Ok()
        {
            ClearCustomerRepo();

            _customerRepo.SaveAll(NewCustomers());

            var actual = _customerRepo.One(x => x.Email.Equals("jdoe@acme.com"));

            Assert.NotNull(actual);
            Assert.Equal(2, actual.Id);

            "Selected entity: ".PrintDump();
            actual.PrintDump();

            "All entities: ".PrintDump();
            DumpRepo();
        }

        [Fact]
        public void RemoveById_Ok()
        {
            ClearCustomerRepo();

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
            ClearCustomerRepo();

            var cust = NewCustomer();
            _customerRepo.Save(cust);

            Assert.Equal(1, _customerRepo.Count());

            _customerRepo.Remove(cust);

            Assert.Equal(0, _customerRepo.Count());
        }

        [Fact]
        public void RemoveByIds_Ok()
        {
            ClearCustomerRepo();

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
            ClearCustomerRepo();

            _customerRepo.SaveAll(NewCustomers());

            Assert.Equal(3, _customerRepo.Count());
            
            _customerRepo.RemoveAll();

            Assert.Equal(0, _customerRepo.Count());
        }

        [Fact]
        public void All_Linq_Ok()
        {
            ClearCustomerRepo();

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

            ClearCustomerRepo();

            var cust = NewCustomer();
            cust.Address = NewAddress();

            var newCust = repo.Save(cust);

            Assert.Equal(1, repo.Count());
            Assert.NotNull(newCust);
            
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

            ClearCustomerRepo();
            _customerRepo.SaveAll(NewCustomers());

            var cust = NewCustomer("MEF", "Head", "mef@meffy.com", "MEFFY");

            MefCustomerRepo.Save(cust);

            Assert.Equal(4, _customerRepo.Count());

            DumpRepo();
        }
    }
}
