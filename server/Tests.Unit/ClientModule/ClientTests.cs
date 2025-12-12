using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Tests.Unit.ClientModule;

[TestClass]
[TestCategory("Client - Client Entity Unit Tests")]
public class ClientTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties_For_Individual_Client()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        string name = "João da Silva";
        string email = "joao.silva@example.com";
        string phoneNumber = "11987654321";
        ClientType clientType = ClientType.Individual;
        string cpf = "12345678901";
        string rg = "123456789";
        string cnh = "12345678901";

        Address address = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua das Flores",
            number: "123"
        );

        // act
        Client client = new Client(
            companyId: companyId,
            name: name,
            phoneNumber: phoneNumber,
            clientType: clientType,
            address: address,
            email: email,
            cpf: cpf,
            rg: rg,
            cnh: cnh
        );

        // assert
        Assert.AreNotEqual(Guid.Empty, client.Id);
        Assert.AreEqual(companyId, client.CompanyId);
        Assert.AreEqual(name, client.Name);
        Assert.AreEqual(email, client.Email);
        Assert.AreEqual(phoneNumber, client.PhoneNumber);
        Assert.AreEqual(clientType, client.ClientType);
        Assert.AreEqual(cpf, client.Cpf);
        Assert.AreEqual(rg, client.Rg);
        Assert.AreEqual(cnh, client.Cnh);
        Assert.IsNull(client.Cnpj);
        Assert.AreEqual(address, client.Address);
    }

    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties_For_LegalEntity_Client()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        string name = "Empresa ABC Ltda";
        string email = "contato@empresaabc.com";
        string phoneNumber = "1133334444";
        ClientType clientType = ClientType.LegalEntity;
        string cnpj = "12345678000199";

        Address address = new Address(
            state: "Rio de Janeiro",
            city: "Rio de Janeiro",
            district: "Copacabana",
            street: "Avenida Atlântica",
            number: "456"
        );

        // act
        Client client = new Client(
            companyId: companyId,
            name: name,
            phoneNumber: phoneNumber,
            clientType: clientType,
            address: address,
            email: email,
            cnpj: cnpj
        );

        // assert
        Assert.AreNotEqual(Guid.Empty, client.Id);
        Assert.AreEqual(companyId, client.CompanyId);
        Assert.AreEqual(name, client.Name);
        Assert.AreEqual(email, client.Email);
        Assert.AreEqual(phoneNumber, client.PhoneNumber);
        Assert.AreEqual(clientType, client.ClientType);
        Assert.AreEqual(cnpj, client.Cnpj);
        Assert.IsNull(client.Cpf);
        Assert.IsNull(client.Rg);
        Assert.IsNull(client.Cnh);
        Assert.AreEqual(address, client.Address);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_And_CompanyId_For_Individual_Client()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Address originalAddress = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua das Flores",
            number: "123"
        );

        Client originalClient = new Client(
            companyId: companyId,
            name: "João da Silva",
            phoneNumber: "11987654321",
            clientType: ClientType.Individual,
            address: originalAddress,
            email: "joao.silva@example.com",
            cpf: "12345678901",
            rg: "123456789",
            cnh: "12345678901"
        );

        Guid originalId = originalClient.Id;
        Guid originalCompanyId = originalClient.CompanyId;

        Address updatedAddress = new Address(
            state: "Rio de Janeiro",
            city: "Rio de Janeiro",
            district: "Copacabana",
            street: "Avenida Atlântica",
            number: "789"
        );

        Client updatedClient = new Client(
            companyId: Guid.NewGuid(),
            name: "João Silva Santos",
            phoneNumber: "11999887766",
            clientType: ClientType.Individual,
            address: updatedAddress,
            email: "joao.santos@example.com",
            cpf: "98765432100",
            rg: "987654321",
            cnh: "98765432100"
        );

        // act
        originalClient.Update(updatedClient);

        // assert
        Assert.AreEqual(updatedClient.Name, originalClient.Name);
        Assert.AreEqual(updatedClient.Email, originalClient.Email);
        Assert.AreEqual(updatedClient.PhoneNumber, originalClient.PhoneNumber);
        Assert.AreEqual(updatedClient.ClientType, originalClient.ClientType);
        Assert.AreEqual(updatedClient.Cpf, originalClient.Cpf);
        Assert.AreEqual(updatedClient.Rg, originalClient.Rg);
        Assert.AreEqual(updatedClient.Cnh, originalClient.Cnh);
        Assert.AreEqual(updatedClient.Cnpj, originalClient.Cnpj);

        Assert.AreEqual(updatedAddress.State, originalClient.Address.State);
        Assert.AreEqual(updatedAddress.City, originalClient.Address.City);
        Assert.AreEqual(updatedAddress.District, originalClient.Address.District);
        Assert.AreEqual(updatedAddress.Street, originalClient.Address.Street);
        Assert.AreEqual(updatedAddress.Number, originalClient.Address.Number);

        Assert.AreEqual(originalId, originalClient.Id);
        Assert.AreEqual(originalCompanyId, originalClient.CompanyId);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_And_CompanyId_For_LegalEntity_Client()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Address originalAddress = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua Comercial",
            number: "100"
        );

        Client originalClient = new Client(
            companyId: companyId,
            name: "Empresa ABC Ltda",
            phoneNumber: "1133334444",
            clientType: ClientType.LegalEntity,
            address: originalAddress,
            email: "contato@empresaabc.com",
            cnpj: "12345678000199"
        );

        Guid originalId = originalClient.Id;
        Guid originalCompanyId = originalClient.CompanyId;

        Address updatedAddress = new Address(
            state: "Minas Gerais",
            city: "Belo Horizonte",
            district: "Savassi",
            street: "Avenida Getúlio Vargas",
            number: "200"
        );

        Client updatedClient = new Client(
            companyId: Guid.NewGuid(),
            name: "Empresa XYZ S.A.",
            phoneNumber: "3144445555",
            clientType: ClientType.LegalEntity,
            address: updatedAddress,
            email: "contato@empresaxyz.com",
            cnpj: "98765432000188"
        );

        // act
        originalClient.Update(updatedClient);

        // assert
        Assert.AreEqual(updatedClient.Name, originalClient.Name);
        Assert.AreEqual(updatedClient.Email, originalClient.Email);
        Assert.AreEqual(updatedClient.PhoneNumber, originalClient.PhoneNumber);
        Assert.AreEqual(updatedClient.ClientType, originalClient.ClientType);
        Assert.AreEqual(updatedClient.Cnpj, originalClient.Cnpj);

        Assert.AreEqual(updatedAddress.State, originalClient.Address.State);
        Assert.AreEqual(updatedAddress.City, originalClient.Address.City);
        Assert.AreEqual(updatedAddress.District, originalClient.Address.District);
        Assert.AreEqual(updatedAddress.Street, originalClient.Address.Street);
        Assert.AreEqual(updatedAddress.Number, originalClient.Address.Number);

        Assert.AreEqual(originalId, originalClient.Id);
        Assert.AreEqual(originalCompanyId, originalClient.CompanyId);
    }

    [TestMethod]
    public void Update_Should_Allow_Changing_ClientType_From_Individual_To_LegalEntity()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Address address = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua Principal",
            number: "50"
        );

        Client originalClient = new Client(
            companyId: companyId,
            name: "João da Silva",
            phoneNumber: "11987654321",
            clientType: ClientType.Individual,
            address: address,
            email: "joao.silva@example.com",
            cpf: "12345678901",
            rg: "123456789",
            cnh: "12345678901"
        );

        Client updatedClient = new Client(
            companyId: companyId,
            name: "Empresa João Silva ME",
            phoneNumber: "11987654321",
            clientType: ClientType.LegalEntity,
            address: address,
            email: "contato@joaosilva.com",
            cnpj: "12345678000199"
        );

        // act
        originalClient.Update(updatedClient);

        // assert
        Assert.AreEqual(ClientType.LegalEntity, originalClient.ClientType);
        Assert.AreEqual("12345678000199", originalClient.Cnpj);
        Assert.IsNull(originalClient.Cpf);
        Assert.IsNull(originalClient.Rg);
        Assert.IsNull(originalClient.Cnh);
    }

    [TestMethod]
    public void Update_Should_Allow_Changing_ClientType_From_LegalEntity_To_Individual()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Address address = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua Principal",
            number: "50"
        );

        Client originalClient = new Client(
            companyId: companyId,
            name: "Empresa ABC Ltda",
            phoneNumber: "1133334444",
            clientType: ClientType.LegalEntity,
            address: address,
            email: "contato@empresaabc.com",
            cnpj: "12345678000199"
        );

        Client updatedClient = new Client(
            companyId: companyId,
            name: "João da Silva",
            phoneNumber: "11987654321",
            clientType: ClientType.Individual,
            address: address,
            email: "joao.silva@example.com",
            cpf: "12345678901",
            rg: "123456789",
            cnh: "12345678901"
        );

        // act
        originalClient.Update(updatedClient);

        // assert
        Assert.AreEqual(ClientType.Individual, originalClient.ClientType);
        Assert.AreEqual("12345678901", originalClient.Cpf);
        Assert.AreEqual("123456789", originalClient.Rg);
        Assert.AreEqual("12345678901", originalClient.Cnh);
        Assert.IsNull(originalClient.Cnpj);
    }
}

[TestClass]
[TestCategory("Client - Address Unit Tests")]
public class AddressTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string state = "São Paulo";
        string city = "São Paulo";
        string district = "Centro";
        string street = "Rua das Flores";
        string number = "123";

        // act
        Address address = new Address(
            state: state,
            city: city,
            district: district,
            street: street,
            number: number
        );

        // assert
        Assert.AreEqual(state, address.State);
        Assert.AreEqual(city, address.City);
        Assert.AreEqual(district, address.District);
        Assert.AreEqual(street, address.Street);
        Assert.AreEqual(number, address.Number);
    }

    [TestMethod]
    public void WithUpdatedValues_Should_Return_New_Address_With_Updated_Properties()
    {
        // arrange
        Address originalAddress = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua das Flores",
            number: "123"
        );

        Address updatedAddress = new Address(
            state: "Rio de Janeiro",
            city: "Rio de Janeiro",
            district: "Copacabana",
            street: "Avenida Atlântica",
            number: "456"
        );

        // act
        Address newAddress = originalAddress.WithUpdatedValues(updatedAddress);

        // assert
        Assert.AreEqual(updatedAddress.State, newAddress.State);
        Assert.AreEqual(updatedAddress.City, newAddress.City);
        Assert.AreEqual(updatedAddress.District, newAddress.District);
        Assert.AreEqual(updatedAddress.Street, newAddress.Street);
        Assert.AreEqual(updatedAddress.Number, newAddress.Number);

        Assert.AreNotSame(originalAddress, newAddress);
        Assert.AreNotSame(updatedAddress, newAddress);
    }

    [TestMethod]
    public void WithUpdatedValues_Should_Create_Independent_Address_Instance()
    {
        // arrange
        Address originalAddress = new Address(
            state: "São Paulo",
            city: "São Paulo",
            district: "Centro",
            street: "Rua das Flores",
            number: "123"
        );

        Address updatedAddress = new Address(
            state: "Minas Gerais",
            city: "Belo Horizonte",
            district: "Savassi",
            street: "Avenida Getúlio Vargas",
            number: "789"
        );

        // act
        Address newAddress = originalAddress.WithUpdatedValues(updatedAddress);

        // assert
        Assert.AreEqual("São Paulo", originalAddress.State);
        Assert.AreEqual("Minas Gerais", newAddress.State);
        Assert.AreNotSame(originalAddress, newAddress);
    }
}
