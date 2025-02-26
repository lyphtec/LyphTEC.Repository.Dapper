namespace LyphTEC.Repository.Dapper.Tests.Domain;

public class Customer : Entity, IAggregateRoot
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Company { get; set; }
    public Address Address { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
}
