using System;

namespace LyphTEC.Repository.Dapper.Tests.Domain;

public class Invoice : Entity
{
    public dynamic CustomerId { get; set; }  // Ref to Customer.Id
    public DateTime InvoiceDate { get; set; }
    public Address BillingAddress { get; set; }
    public decimal Total { get; set; }
}
