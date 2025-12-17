using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.DriverModule;

[TestClass]
[TestCategory("DriverOrmRepository Infrastructure - Integration Tests")]
public class DriverOrmRepositoryTests : TestFixture
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

    private static Client CreateClient(
        Guid companyId,
        string name = "Cliente Teste",
        string phoneNumber = "(48) 99999-9999",
        string email = "cliente@teste.com")
    {
        return new Client(
            companyId: companyId,
            name: name,
            phoneNumber: phoneNumber,
            clientType: ClientType.Individual,
            address: CreateAddress(),
            email: email,
            cpf: "11122233344",
            rg: "123456789",
            cnh: "12345678900",
            cnpj: null);
    }

    private static Driver CreateDriver(
        Guid companyId,
        Guid clientId,
        string name,
        string phoneNumber,
        string cpf,
        string cnh,
        string email,
        DateOnly? cnhExpirationDate = null,
        bool isClientAlsoDriver = false)
    {
        DateOnly effectiveExpirationDate =
            cnhExpirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        return new Driver(
            companyId: companyId,
            clientId: clientId,
            name: name,
            phoneNumber: phoneNumber,
            cpf: cpf,
            cnh: cnh,
            cnhExpirationDate: effectiveExpirationDate,
            email: email,
            isClientAlsoDriver: isClientAlsoDriver);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_Should_Return_True_When_Driver_With_Same_Email_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string driverEmail = "driver@teste.com";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Email",
            phoneNumber: "(11) 90000-0001",
            cpf: "12312312312",
            cnh: "CNH-EMAIL-1",
            email: driverEmail);

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByEmailAsync(driverEmail);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_Should_Return_False_When_Driver_With_Email_Does_Not_Exist()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        Driver existingDriver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Existente",
            phoneNumber: "(11) 90000-0002",
            cpf: "23423423423",
            cnh: "CNH-EMAIL-2",
            email: "existente@teste.com");

        dbContext.Drivers.Add(existingDriver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByEmailAsync("naoexiste@teste.com");

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_Should_Return_False_When_Email_Is_Empty_Or_Whitespace()
    {
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        bool existsForEmpty = await driverRepository.ExistsByEmailAsync(string.Empty);
        bool existsForWhitespace = await driverRepository.ExistsByEmailAsync("   ");

        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_WithIgnoreId_Should_Return_False_When_Only_Driver_With_Email_Is_Self()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string driverEmail = "unico@teste.com";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Único",
            phoneNumber: "(11) 90000-0003",
            cpf: "34534534534",
            cnh: "CNH-EMAIL-3",
            email: driverEmail);

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByEmailAsync(driverEmail, driver.Id);

        Assert.IsFalse(exists, "Não deveria considerar o próprio condutor como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByEmailAsync_WithIgnoreId_Should_Return_True_When_Other_Driver_With_Same_Email_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string driverEmail = "duplicado@teste.com";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver",
            phoneNumber: "(11) 90000-0004",
            cpf: "45645645645",
            cnh: "CNH-EMAIL-4",
            email: driverEmail);

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByEmailAsync(driverEmail, Guid.NewGuid());

        Assert.IsTrue(exists, "Deveria detectar outro condutor com o mesmo e-mail.");
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_Should_Return_True_When_Driver_With_Same_PhoneNumber_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string phoneNumber = "(48) 99999-0001";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Telefone",
            phoneNumber: phoneNumber,
            cpf: "56756756756",
            cnh: "CNH-PHONE-1",
            email: "phone@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByPhoneNumberAsync(phoneNumber);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_Should_Return_False_When_Driver_With_PhoneNumber_Does_Not_Exist()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        Driver existingDriver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Existente",
            phoneNumber: "(48) 99999-0002",
            cpf: "67867867867",
            cnh: "CNH-PHONE-2",
            email: "existente-phone@teste.com");

        dbContext.Drivers.Add(existingDriver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByPhoneNumberAsync("(48) 99999-9999");

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_Should_Return_False_When_PhoneNumber_Is_Empty_Or_Whitespace()
    {
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        bool existsForEmpty = await driverRepository.ExistsByPhoneNumberAsync(string.Empty);
        bool existsForWhitespace = await driverRepository.ExistsByPhoneNumberAsync("   ");

        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_WithIgnoreId_Should_Return_False_When_Only_Driver_With_PhoneNumber_Is_Self()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string phoneNumber = "(48) 99999-0003";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Único",
            phoneNumber: phoneNumber,
            cpf: "78978978978",
            cnh: "CNH-PHONE-3",
            email: "unico-phone@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByPhoneNumberAsync(phoneNumber, driver.Id);

        Assert.IsFalse(exists, "Não deveria considerar o próprio condutor como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByPhoneNumberAsync_WithIgnoreId_Should_Return_True_When_Other_Driver_With_Same_PhoneNumber_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string phoneNumber = "(48) 99999-0004";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver",
            phoneNumber: phoneNumber,
            cpf: "89089089089",
            cnh: "CNH-PHONE-4",
            email: "phone2@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByPhoneNumberAsync(phoneNumber, Guid.NewGuid());

        Assert.IsTrue(exists, "Deveria detectar outro condutor com o mesmo telefone.");
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_Should_Return_True_When_Driver_With_Same_Cpf_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string cpf = "11111111111";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver CPF",
            phoneNumber: "(11) 90000-0101",
            cpf: cpf,
            cnh: "CNH-CPF-1",
            email: "cpf@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCpfAsync(cpf);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_Should_Return_False_When_Driver_With_Cpf_Does_Not_Exist()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        Driver existingDriver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Existente",
            phoneNumber: "(11) 90000-0102",
            cpf: "22222222222",
            cnh: "CNH-CPF-2",
            email: "existente-cpf@teste.com");

        dbContext.Drivers.Add(existingDriver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCpfAsync("33333333333");

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_Should_Return_False_When_Cpf_Is_Empty_Or_Whitespace()
    {
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        bool existsForEmpty = await driverRepository.ExistsByCpfAsync(string.Empty);
        bool existsForWhitespace = await driverRepository.ExistsByCpfAsync("   ");

        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_WithIgnoreId_Should_Return_False_When_Only_Driver_With_Cpf_Is_Self()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string cpf = "44444444444";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Único",
            phoneNumber: "(11) 90000-0103",
            cpf: cpf,
            cnh: "CNH-CPF-3",
            email: "unico-cpf@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCpfAsync(cpf, driver.Id);

        Assert.IsFalse(exists, "Não deveria considerar o próprio condutor como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByCpfAsync_WithIgnoreId_Should_Return_True_When_Other_Driver_With_Same_Cpf_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string cpf = "55555555555";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver",
            phoneNumber: "(11) 90000-0104",
            cpf: cpf,
            cnh: "CNH-CPF-4",
            email: "cpf2@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCpfAsync(cpf, Guid.NewGuid());

        Assert.IsTrue(exists, "Deveria detectar outro condutor com o mesmo CPF.");
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_Should_Return_True_When_Driver_With_Same_Cnh_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string cnh = "CNH-0000001";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver CNH",
            phoneNumber: "(11) 90000-0201",
            cpf: "66666666666",
            cnh: cnh,
            email: "cnh@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCnhAsync(cnh);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_Should_Return_False_When_Driver_With_Cnh_Does_Not_Exist()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        Driver existingDriver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Existente",
            phoneNumber: "(11) 90000-0202",
            cpf: "77777777777",
            cnh: "CNH-EXISTE",
            email: "existente-cnh@teste.com");

        dbContext.Drivers.Add(existingDriver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCnhAsync("CNH-NAO-EXISTE");

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_Should_Return_False_When_Cnh_Is_Empty_Or_Whitespace()
    {
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        bool existsForEmpty = await driverRepository.ExistsByCnhAsync(string.Empty);
        bool existsForWhitespace = await driverRepository.ExistsByCnhAsync("   ");

        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_WithIgnoreId_Should_Return_False_When_Only_Driver_With_Cnh_Is_Self()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string cnh = "CNH-UNICO";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver Único",
            phoneNumber: "(11) 90000-0203",
            cpf: "88888888888",
            cnh: cnh,
            email: "unico-cnh@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCnhAsync(cnh, driver.Id);

        Assert.IsFalse(exists, "Não deveria considerar o próprio condutor como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByCnhAsync_WithIgnoreId_Should_Return_True_When_Other_Driver_With_Same_Cnh_Exists()
    {
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryDriver driverRepository = _driverRepository ?? throw new InvalidOperationException("Driver repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        string cnh = "CNH-DUPLICADO";

        Driver driver = CreateDriver(
            companyId: companyId,
            clientId: client.Id,
            name: "Driver",
            phoneNumber: "(11) 90000-0204",
            cpf: "99999999999",
            cnh: cnh,
            email: "cnh2@teste.com");

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync();

        bool exists = await driverRepository.ExistsByCnhAsync(cnh, Guid.NewGuid());

        Assert.IsTrue(exists, "Deveria detectar outro condutor com a mesma CNH.");
    }
}