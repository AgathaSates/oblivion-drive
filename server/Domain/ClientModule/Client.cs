using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.ClientModule;
public class Client : TenantEntity<Client>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }

    public ClientType ClientType { get; private set; }

    //Pessoa Física
    public string? Cpf { get; private set; }
    public string? Rg { get; private set; }
    public string? Cnh { get; private set; }

    //Pessoa Jurídica
    public string? Cnpj { get; private set; }

    public Address Address { get; private set; }

    public ICollection<Driver> Drivers { get; private set; } = new List<Driver>();

    [ExcludeFromCodeCoverage]
    private Client() { }

    public Client(Guid companyId, string name, string phoneNumber, ClientType clientType, Address address,
        string email, string? cpf = null, string? rg = null, string? cnh = null, string? cnpj = null)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;

        Name = name;
        PhoneNumber = phoneNumber;
        ClientType = clientType;
        Address = address;
        Email = email;

        Cpf = cpf;
        Rg = rg;
        Cnh = cnh;
        Cnpj = cnpj;
    }

    public override void Update(Client updatedEntity)
    {
        Name = updatedEntity.Name;
        Email = updatedEntity.Email;
        PhoneNumber = updatedEntity.PhoneNumber;
        ClientType = updatedEntity.ClientType;

        Address = Address.WithUpdatedValues(updatedEntity.Address);

        Cpf = updatedEntity.Cpf;
        Rg = updatedEntity.Rg;
        Cnh = updatedEntity.Cnh;
        Cnpj = updatedEntity.Cnpj;
    }
}

public class Address
{
    public string State { get; private set; }
    public string City { get; private set; }
    public string District { get; private set; }
    public string Street { get; private set; }
    public string Number { get; private set; }

    [ExcludeFromCodeCoverage]
    private Address() { }

    public Address(string state, string city, string district, string street, string number)
    {
        State = state;
        City = city;
        District = district;
        Street = street;
        Number = number;
    }

    public Address WithUpdatedValues(Address updatedAddress)
        => new(
            updatedAddress.State,
            updatedAddress.City,
            updatedAddress.District,
            updatedAddress.Street,
            updatedAddress.Number);
}