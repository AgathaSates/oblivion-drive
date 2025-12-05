using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.DriverModule;
public class Driver : TenantEntity<Driver>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }

    public string Cpf { get; private set; }
    public string Cnh { get; private set; }
    public DateOnly CnhExpirationDate { get; private set; }

    public Guid ClientId { get; private set; }
    public Client Client { get; private set; } = null!;

    public bool IsClientAlsoDriver { get; private set; }

    [ExcludeFromCodeCoverage]
    private Driver() { }

    public Driver(Guid companyId, Guid clientId,
        string name, string phoneNumber, string cpf, string cnh,
        DateOnly cnhExpirationDate, string email, bool isClientAlsoDriver = false)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;

        ClientId = clientId;

        Name = name;
        PhoneNumber = phoneNumber;
        Cpf = cpf;
        Cnh = cnh;
        CnhExpirationDate = cnhExpirationDate;
        Email = email;
        IsClientAlsoDriver = isClientAlsoDriver;
    }

    public override void Update(Driver updatedEntity)
    {
        Name = updatedEntity.Name;
        Email = updatedEntity.Email;
        PhoneNumber = updatedEntity.PhoneNumber;

        Cpf = updatedEntity.Cpf;
        Cnh = updatedEntity.Cnh;
        CnhExpirationDate = updatedEntity.CnhExpirationDate;

        ClientId = updatedEntity.ClientId;
        IsClientAlsoDriver = updatedEntity.IsClientAlsoDriver;
    }
}