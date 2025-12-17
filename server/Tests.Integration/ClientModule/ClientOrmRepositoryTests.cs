using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;


namespace OblivionDrive.Tests.Integration.ClientModule;

[TestClass]
[TestCategory("ClientOrmRepository Infrastructure - Integration Tests")]
public class ClientOrmRepositoryTests : TestFixture
{
    private static Address CreateAddress(
       string state = "SC",
       string city = "Florianópolis",
       string district = "Centro",
       string street = "Rua Teste",
       string number = "123")
    {
        return new Address(
            state: state,
            city: city,
            district: district,
            street: street,
            number: number);
    }

    private static Client CreateIndividualClient(
        Guid companyId,
        string name,
        string phoneNumber,
        string email,
        string cpf,
        string rg = "123456789",
        string cnh = "12345678900")
    {
        return new Client(
            companyId: companyId,
            name: name,
            phoneNumber: phoneNumber,
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: email,
            cpf: cpf,
            rg: rg,
            cnh: cnh,
            cnpj: null);
    }

    private static Client CreateLegalEntityClient(
        Guid companyId,
        string name,
        string phoneNumber,
        string email,
        string cnpj)
    {
        return new Client(
            companyId: companyId,
            name: name,
            phoneNumber: phoneNumber,
            clientType: ClientType.LegalEntity,
            address: CreateAddress(),
            email: email,
            cpf: null,
            rg: null,
            cnh: null,
            cnpj: cnpj);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_Should_Return_True_When_Client_With_Same_Email_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string clientEmail = "cliente@teste.com";

        Client client = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Email",
            phoneNumber: "(11) 99999-9999",
            email: clientEmail,
            cpf: "11122233344");

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByEmailAsync(clientEmail);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_Should_Return_False_When_Client_With_Email_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client existingClient = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Existente",
            phoneNumber: "(11) 98888-7777",
            email: "existente@teste.com",
            cpf: "55566677788");

        dbContext.Clients.Add(existingClient);
        await dbContext.SaveChangesAsync();

        string searchedEmail = "naoexiste@teste.com";

        // act
        bool exists = await clientRepository.ExistsByEmailAsync(searchedEmail);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_Should_Return_False_When_Email_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        // act
        bool existsForEmpty = await clientRepository.ExistsByEmailAsync(string.Empty);
        bool existsForWhitespace = await clientRepository.ExistsByEmailAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_WithIgnoreId_Should_Return_False_When_Only_Client_With_Email_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string clientEmail = "unico@teste.com";

        Client client = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Único",
            phoneNumber: "(11) 97777-6666",
            email: clientEmail,
            cpf: "99988877766");

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByEmailAsync(clientEmail, client.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cliente como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_WithIgnoreId_Should_Return_True_When_Other_Client_With_Same_Email_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedEmail = "duplicado@teste.com";

        Client client1 = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente 1",
            phoneNumber: "(11) 91111-1111",
            email: duplicatedEmail,
            cpf: "10101010101");

        Client client2 = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente 2",
            phoneNumber: "(11) 92222-2222",
            email: duplicatedEmail,
            cpf: "20202020202");

        dbContext.Clients.Add(client1);
        dbContext.Clients.Add(client2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByEmailAsync(duplicatedEmail, client1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cliente com o mesmo e-mail.");
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_Should_Return_True_When_Client_With_Same_PhoneNumber_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string phoneNumber = "(48) 99999-0001";

        Client client = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Telefone",
            phoneNumber: phoneNumber,
            email: "telefone@teste.com",
            cpf: "33344455566");

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByPhoneNumberAsync(phoneNumber);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_Should_Return_False_When_Client_With_PhoneNumber_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client existingClient = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Existente",
            phoneNumber: "(48) 99999-0002",
            email: "existe@teste.com",
            cpf: "44455566677");

        dbContext.Clients.Add(existingClient);
        await dbContext.SaveChangesAsync();

        string searchedPhoneNumber = "(48) 99999-9999";

        // act
        bool exists = await clientRepository.ExistsByPhoneNumberAsync(searchedPhoneNumber);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_Should_Return_False_When_PhoneNumber_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        // act
        bool existsForEmpty = await clientRepository.ExistsByPhoneNumberAsync(string.Empty);
        bool existsForWhitespace = await clientRepository.ExistsByPhoneNumberAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_WithIgnoreId_Should_Return_False_When_Only_Client_With_PhoneNumber_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string phoneNumber = "(48) 99999-0003";

        Client client = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Único",
            phoneNumber: phoneNumber,
            email: "unico-telefone@teste.com",
            cpf: "77788899900");

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByPhoneNumberAsync(phoneNumber, client.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cliente como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_WithIgnoreId_Should_Return_True_When_Other_Client_With_Same_PhoneNumber_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedPhoneNumber = "(48) 99999-0004";

        Client client1 = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente 1",
            phoneNumber: duplicatedPhoneNumber,
            email: "cliente1@teste.com",
            cpf: "12121212121");

        Client client2 = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente 2",
            phoneNumber: duplicatedPhoneNumber,
            email: "cliente2@teste.com",
            cpf: "34343434343");

        dbContext.Clients.Add(client1);
        dbContext.Clients.Add(client2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByPhoneNumberAsync(duplicatedPhoneNumber, client1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cliente com o mesmo telefone.");
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_Should_Return_True_When_Client_With_Same_Cpf_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string cpf = "12312312312";

        Client client = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente CPF",
            phoneNumber: "(11) 90000-0001",
            email: "cpf@teste.com",
            cpf: cpf);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCpfAsync(cpf);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_Should_Return_False_When_Client_With_Cpf_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client existingClient = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Existente",
            phoneNumber: "(11) 90000-0002",
            email: "existente-cpf@teste.com",
            cpf: "98798798798");

        dbContext.Clients.Add(existingClient);
        await dbContext.SaveChangesAsync();

        string searchedCpf = "11111111111";

        // act
        bool exists = await clientRepository.ExistsByCpfAsync(searchedCpf);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_Should_Return_False_When_Cpf_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        // act
        bool existsForEmpty = await clientRepository.ExistsByCpfAsync(string.Empty);
        bool existsForWhitespace = await clientRepository.ExistsByCpfAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_WithIgnoreId_Should_Return_False_When_Only_Client_With_Cpf_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string cpf = "22233344455";

        Client client = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente Único",
            phoneNumber: "(11) 90000-0003",
            email: "unico-cpf@teste.com",
            cpf: cpf);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCpfAsync(cpf, client.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cliente como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_WithIgnoreId_Should_Return_True_When_Other_Client_With_Same_Cpf_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedCpf = "33344455566";

        Client client1 = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente 1",
            phoneNumber: "(11) 90000-0004",
            email: "cpf1@teste.com",
            cpf: duplicatedCpf);

        Client client2 = CreateIndividualClient(
            companyId: companyId,
            name: "Cliente 2",
            phoneNumber: "(11) 90000-0005",
            email: "cpf2@teste.com",
            cpf: duplicatedCpf);

        dbContext.Clients.Add(client1);
        dbContext.Clients.Add(client2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCpfAsync(duplicatedCpf, client1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cliente com o mesmo CPF.");
    }

    [TestMethod]
    public async Task ExistsByRgAsync_Should_Return_True_When_Client_With_Same_Rg_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string rg = "RG-0000001";

        Client client = new Client(
            companyId: companyId,
            name: "Cliente RG",
            phoneNumber: "(11) 90000-0101",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "rg@teste.com",
            cpf: "44455566677",
            rg: rg,
            cnh: "11111111111",
            cnpj: null);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByRgAsync(rg);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByRgAsync_Should_Return_False_When_Client_With_Rg_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client existingClient = new Client(
            companyId: companyId,
            name: "Cliente Existente",
            phoneNumber: "(11) 90000-0102",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "existente-rg@teste.com",
            cpf: "55566677788",
            rg: "RG-EXISTE",
            cnh: "22222222222",
            cnpj: null);

        dbContext.Clients.Add(existingClient);
        await dbContext.SaveChangesAsync();

        string searchedRg = "RG-NAO-EXISTE";

        // act
        bool exists = await clientRepository.ExistsByRgAsync(searchedRg);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByRgAsync_Should_Return_False_When_Rg_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        // act
        bool existsForEmpty = await clientRepository.ExistsByRgAsync(string.Empty);
        bool existsForWhitespace = await clientRepository.ExistsByRgAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByRgAsync_WithIgnoreId_Should_Return_False_When_Only_Client_With_Rg_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string rg = "RG-UNICO";

        Client client = new Client(
            companyId: companyId,
            name: "Cliente Único",
            phoneNumber: "(11) 90000-0103",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "unico-rg@teste.com",
            cpf: "66677788899",
            rg: rg,
            cnh: "33333333333",
            cnpj: null);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByRgAsync(rg, client.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cliente como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByRgAsync_WithIgnoreId_Should_Return_True_When_Other_Client_With_Same_Rg_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedRg = "RG-DUPLICADO";

        Client client1 = new Client(
            companyId: companyId,
            name: "Cliente 1",
            phoneNumber: "(11) 90000-0104",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "rg1@teste.com",
            cpf: "77788899900",
            rg: duplicatedRg,
            cnh: "44444444444",
            cnpj: null);

        Client client2 = new Client(
            companyId: companyId,
            name: "Cliente 2",
            phoneNumber: "(11) 90000-0105",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "rg2@teste.com",
            cpf: "88899900011",
            rg: duplicatedRg,
            cnh: "55555555555",
            cnpj: null);

        dbContext.Clients.Add(client1);
        dbContext.Clients.Add(client2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByRgAsync(duplicatedRg, client1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cliente com o mesmo RG.");
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_Should_Return_True_When_Client_With_Same_Cnh_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string cnh = "CNH-0000001";

        Client client = new Client(
            companyId: companyId,
            name: "Cliente CNH",
            phoneNumber: "(11) 90000-0201",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "cnh@teste.com",
            cpf: "99900011122",
            rg: "RG-CNH-1",
            cnh: cnh,
            cnpj: null);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCnhAsync(cnh);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_Should_Return_False_When_Client_With_Cnh_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client existingClient = new Client(
            companyId: companyId,
            name: "Cliente Existente",
            phoneNumber: "(11) 90000-0202",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "existente-cnh@teste.com",
            cpf: "11100099988",
            rg: "RG-CNH-EXISTE",
            cnh: "CNH-EXISTE",
            cnpj: null);

        dbContext.Clients.Add(existingClient);
        await dbContext.SaveChangesAsync();

        string searchedCnh = "CNH-NAO-EXISTE";

        // act
        bool exists = await clientRepository.ExistsByCnhAsync(searchedCnh);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_Should_Return_False_When_Cnh_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        // act
        bool existsForEmpty = await clientRepository.ExistsByCnhAsync(string.Empty);
        bool existsForWhitespace = await clientRepository.ExistsByCnhAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_WithIgnoreId_Should_Return_False_When_Only_Client_With_Cnh_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string cnh = "CNH-UNICO";

        Client client = new Client(
            companyId: companyId,
            name: "Cliente Único",
            phoneNumber: "(11) 90000-0203",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "unico-cnh@teste.com",
            cpf: "22233344455",
            rg: "RG-CNH-UNICO",
            cnh: cnh,
            cnpj: null);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCnhAsync(cnh, client.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cliente como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_WithIgnoreId_Should_Return_True_When_Other_Client_With_Same_Cnh_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedCnh = "CNH-DUPLICADO";

        Client client1 = new Client(
            companyId: companyId,
            name: "Cliente 1",
            phoneNumber: "(11) 90000-0204",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "cnh1@teste.com",
            cpf: "33344455566",
            rg: "RG-CNH-1",
            cnh: duplicatedCnh,
            cnpj: null);

        Client client2 = new Client(
            companyId: companyId,
            name: "Cliente 2",
            phoneNumber: "(11) 90000-0205",
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: "cnh2@teste.com",
            cpf: "44455566677",
            rg: "RG-CNH-2",
            cnh: duplicatedCnh,
            cnpj: null);

        dbContext.Clients.Add(client1);
        dbContext.Clients.Add(client2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCnhAsync(duplicatedCnh, client1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cliente com a mesma CNH.");
    }

    [TestMethod]
    public async Task ExistsByCnpjAsync_Should_Return_True_When_Client_With_Same_Cnpj_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string cnpj = "11222333000144";

        Client client = CreateLegalEntityClient(
            companyId: companyId,
            name: "Cliente PJ",
            phoneNumber: "(11) 90000-0301",
            email: "pj@teste.com",
            cnpj: cnpj);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCnpjAsync(cnpj);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByCnpjAsync_Should_Return_False_When_Client_With_Cnpj_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client existingClient = CreateLegalEntityClient(
            companyId: companyId,
            name: "Cliente PJ Existente",
            phoneNumber: "(11) 90000-0302",
            email: "pj-existente@teste.com",
            cnpj: "99888777000166");

        dbContext.Clients.Add(existingClient);
        await dbContext.SaveChangesAsync();

        string searchedCnpj = "00000000000000";

        // act
        bool exists = await clientRepository.ExistsByCnpjAsync(searchedCnpj);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByCnpjAsync_Should_Return_False_When_Cnpj_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        // act
        bool existsForEmpty = await clientRepository.ExistsByCnpjAsync(string.Empty);
        bool existsForWhitespace = await clientRepository.ExistsByCnpjAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByCnpjAsync_WithIgnoreId_Should_Return_False_When_Only_Client_With_Cnpj_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string cnpj = "12345000000199";

        Client client = CreateLegalEntityClient(
            companyId: companyId,
            name: "Cliente PJ Único",
            phoneNumber: "(11) 90000-0303",
            email: "pj-unico@teste.com",
            cnpj: cnpj);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCnpjAsync(cnpj, client.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cliente como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByCnpjAsync_WithIgnoreId_Should_Return_True_When_Other_Client_With_Same_Cnpj_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryClient clientRepository =
            _clientRepository ?? throw new InvalidOperationException("Client repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedCnpj = "55666777000188";

        Client client1 = CreateLegalEntityClient(
            companyId: companyId,
            name: "Cliente PJ 1",
            phoneNumber: "(11) 90000-0304",
            email: "pj1@teste.com",
            cnpj: duplicatedCnpj);

        Client client2 = CreateLegalEntityClient(
            companyId: companyId,
            name: "Cliente PJ 2",
            phoneNumber: "(11) 90000-0305",
            email: "pj2@teste.com",
            cnpj: duplicatedCnpj);

        dbContext.Clients.Add(client1);
        dbContext.Clients.Add(client2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await clientRepository.ExistsByCnpjAsync(duplicatedCnpj, client1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cliente com o mesmo CNPJ.");
    }
}