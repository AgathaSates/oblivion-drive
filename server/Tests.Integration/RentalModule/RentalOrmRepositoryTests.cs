using System.Reflection;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.RentalModule;

[TestClass]
[TestCategory("RentalOrmRepository Infrastructure - Integration Tests")]
public class RentalOrmRepositoryTests : TestFixture
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
        string email = "cliente@teste.com",
        string phoneNumber = "(48) 99999-9999")
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
        string name = "Condutor Teste",
        string email = "condutor@teste.com",
        string phoneNumber = "(48) 98888-7777",
        string cpf = "12312312312",
        string cnh = "CNH-0000001",
        DateOnly? cnhExpirationDate = null)
    {
        DateOnly effectiveCnhExpirationDate =
            cnhExpirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        return new Driver(
            companyId: companyId,
            clientId: clientId,
            name: name,
            phoneNumber: phoneNumber,
            cpf: cpf,
            cnh: cnh,
            cnhExpirationDate: effectiveCnhExpirationDate,
            email: email,
            isClientAlsoDriver: false);
    }

    private static VehicleGroup CreateVehicleGroup(Guid companyId, string name = "Grupo Teste")
    {
        return new VehicleGroup(
            name: name,
            companyId: companyId);
    }

    private static Vehicle CreateVehicle(
        Guid companyId,
        Guid vehicleGroupId,
        string licensePlate,
        string brand = "Toyota",
        string model = "Corolla",
        string color = "White",
        FuelType fuelType = FuelType.Gasoline,
        decimal fuelTankCapacityInLiters = 55.5m,
        int? year = null)
    {
        return new Vehicle(
            licensePlate: licensePlate,
            brand: brand,
            model: model,
            color: color,
            fuelType: fuelType,
            fuelTankCapacityInLiters: fuelTankCapacityInLiters,
            year: year ?? DateTime.UtcNow.Year,
            vehicleGroupId: vehicleGroupId,
            companyId: companyId);
    }

    private static Rental CreateRental(
        Guid companyId,
        Guid clientId,
        Guid driverId,
        Guid vehicleId,
        DateOnly startDate,
        DateOnly expectedReturnDate,
        IEnumerable<Guid>? serviceIds = null,
        decimal servicesTotalPrice = 0m,
        decimal insuranceTotalPrice = 0m,
        decimal rentalBasePrice = 0m,
        decimal estimatedRentalAmount = 0m,
        int estimatedTotalKilometers = 100)
    {
        return new Rental(
            companyId: companyId,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicleId,
            planType: default,
            startDate: startDate,
            expectedReturnDate: expectedReturnDate,
            insuranceDailyPricePerPerson: 0m,
            insurancePersonsCount: 0,
            estimatedTotalKilometers: estimatedTotalKilometers,
            servicesTotalPrice: servicesTotalPrice,
            insuranceTotalPrice: insuranceTotalPrice,
            rentalBasePrice: rentalBasePrice,
            estimatedRentalAmount: estimatedRentalAmount,
            serviceIds: serviceIds);
    }

    private static void CompleteRental(
        Rental rental,
        DateOnly actualReturnDate,
        decimal grossRentalAmount,
        bool hasDamage = false,
        Guid? couponId = null,
        decimal couponDiscountAmount = 0m)
    {
        rental.CompleteReturn(
            actualReturnDate: actualReturnDate,
            initialOdometerInKm: 1000,
            currentOdometerInKm: 1100,
            isFuelTankFullOnReturn: true,
            hasDamage: hasDamage,
            rentalBasePrice: 100m,
            insuranceTotalPrice: 0m,
            servicesTotalPrice: 0m,
            fuelChargePrice: 0m,
            penaltyPrice: 0m,
            grossRentalAmount: grossRentalAmount,
            couponId: couponId,
            couponDiscountAmount: couponDiscountAmount);
    }

    private static Guid ExtractSingleGuid(object instance)
    {
        PropertyInfo? guidProperty = instance
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == typeof(Guid));

        if (guidProperty is null)
            throw new InvalidOperationException("RentalSummaryRow should expose a public Guid property.");

        object? value = guidProperty.GetValue(instance);

        if (value is not Guid guidValue)
            throw new InvalidOperationException("Expected Guid property value was not a Guid.");

        return guidValue;
    }

    private static DateOnly ExtractStartDate(object instance)
    {
        PropertyInfo? startDateProperty = instance
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.PropertyType == typeof(DateOnly) &&
                p.Name.Contains("Start", StringComparison.OrdinalIgnoreCase));

        if (startDateProperty is null)
            throw new InvalidOperationException("RentalSummaryRow should expose a public DateOnly StartDate-like property.");

        object? value = startDateProperty.GetValue(instance);

        if (value is not DateOnly dateValue)
            throw new InvalidOperationException("Expected StartDate-like property value was not a DateOnly.");

        return dateValue;
    }

    private static IReadOnlyCollection<string> ExtractStringValues(object instance)
    {
        return instance
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.GetValue(instance))
            .OfType<string>()
            .ToArray();
    }

    private static IReadOnlyCollection<decimal> ExtractDecimalValues(object instance)
    {
        return instance
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(decimal))
            .Select(p => p.GetValue(instance))
            .OfType<decimal>()
            .ToArray();
    }

    [TestMethod]
    public async Task ExistsOpenRentalForVehicleAsync_Should_Return_True_When_Open_Rental_For_Vehicle_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId, "Grupo Alvo");
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "AAA1A11");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 1, 10),
            expectedReturnDate: new DateOnly(2025, 1, 15));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalForVehicleAsync(vehicle.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalForVehicleAsync_Should_Return_False_When_Only_Completed_Rental_For_Vehicle_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId, "Grupo Alvo");
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "BBB2B22");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental completedRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 2, 1),
            expectedReturnDate: new DateOnly(2025, 2, 5));

        CompleteRental(
            rental: completedRental,
            actualReturnDate: new DateOnly(2025, 2, 5),
            grossRentalAmount: 1500m);

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(completedRental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalForVehicleAsync(vehicle.Id);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsForVehicleGroupAsync_Should_Return_True_When_Rental_For_VehicleGroup_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup targetVehicleGroup = CreateVehicleGroup(companyId, "Grupo Alvo");
        VehicleGroup otherVehicleGroup = CreateVehicleGroup(companyId, "Outro Grupo");

        Vehicle targetVehicle = CreateVehicle(companyId, targetVehicleGroup.Id, licensePlate: "CCC3C33");
        Vehicle otherVehicle = CreateVehicle(companyId, otherVehicleGroup.Id, licensePlate: "DDD4D44");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rentalForTargetGroup = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: targetVehicle.Id,
            startDate: new DateOnly(2025, 3, 1),
            expectedReturnDate: new DateOnly(2025, 3, 3));

        Rental rentalForOtherGroup = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: otherVehicle.Id,
            startDate: new DateOnly(2025, 3, 10),
            expectedReturnDate: new DateOnly(2025, 3, 12));

        dbContext.VehicleGroups.Add(targetVehicleGroup);
        dbContext.VehicleGroups.Add(otherVehicleGroup);
        dbContext.Vehicles.Add(targetVehicle);
        dbContext.Vehicles.Add(otherVehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rentalForTargetGroup);
        dbContext.Rentals.Add(rentalForOtherGroup);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsForVehicleGroupAsync(targetVehicleGroup.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsForVehicleGroupAsync_Should_Return_False_When_No_Rental_For_VehicleGroup_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroupWithRental = CreateVehicleGroup(companyId, "Grupo Com Aluguel");
        VehicleGroup vehicleGroupWithoutRental = CreateVehicleGroup(companyId, "Grupo Sem Aluguel");

        Vehicle vehicleWithRental = CreateVehicle(companyId, vehicleGroupWithRental.Id, licensePlate: "EEE5E55");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicleWithRental.Id,
            startDate: new DateOnly(2025, 4, 1),
            expectedReturnDate: new DateOnly(2025, 4, 2));

        dbContext.VehicleGroups.Add(vehicleGroupWithRental);
        dbContext.VehicleGroups.Add(vehicleGroupWithoutRental);
        dbContext.Vehicles.Add(vehicleWithRental);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsForVehicleGroupAsync(vehicleGroupWithoutRental.Id);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalForClientAsync_Should_Return_True_When_Open_Rental_For_Client_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "FFF6F66");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental openRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 5, 1),
            expectedReturnDate: new DateOnly(2025, 5, 10));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(openRental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalForClientAsync(client.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalForClientAsync_Should_Return_False_When_Only_Completed_Rental_For_Client_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "GGG7G77");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental completedRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 6, 1),
            expectedReturnDate: new DateOnly(2025, 6, 2));

        CompleteRental(
            rental: completedRental,
            actualReturnDate: new DateOnly(2025, 6, 2),
            grossRentalAmount: 1200m);

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(completedRental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalForClientAsync(client.Id);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalForDriverAsync_Should_Return_True_When_Open_Rental_For_Driver_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "HHH8H88");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental openRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 7, 1),
            expectedReturnDate: new DateOnly(2025, 7, 5));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(openRental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalForDriverAsync(driver.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalForDriverAsync_Should_Return_False_When_Only_Completed_Rental_For_Driver_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "III9I99");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental completedRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 8, 1),
            expectedReturnDate: new DateOnly(2025, 8, 3));

        CompleteRental(
            rental: completedRental,
            actualReturnDate: new DateOnly(2025, 8, 3),
            grossRentalAmount: 1800m);

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(completedRental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalForDriverAsync(driver.Id);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalUsingServiceAsync_Should_Return_True_When_Open_Rental_Includes_Service()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();
        Guid targetServiceId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "JJJ1J11");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental openRentalWithService = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 9, 1),
            expectedReturnDate: new DateOnly(2025, 9, 2),
            serviceIds: new[] { targetServiceId });

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(openRentalWithService);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalUsingServiceAsync(targetServiceId);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsOpenRentalUsingServiceAsync_Should_Return_False_When_Service_Is_Only_In_Completed_Rentals()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();
        Guid targetServiceId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "KKK2K22");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental completedRentalWithService = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 10, 1),
            expectedReturnDate: new DateOnly(2025, 10, 2),
            serviceIds: new[] { targetServiceId });

        CompleteRental(
            rental: completedRentalWithService,
            actualReturnDate: new DateOnly(2025, 10, 2),
            grossRentalAmount: 1300m);

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(completedRentalWithService);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsOpenRentalUsingServiceAsync(targetServiceId);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsAnyRentalForVehicleAsync_Should_Return_True_When_Any_Rental_For_Vehicle_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "LLL3L33");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 11, 1),
            expectedReturnDate: new DateOnly(2025, 11, 3));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsAnyRentalForVehicleAsync(vehicle.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAnyRentalForVehicleAsync_Should_Return_False_When_No_Rental_For_Vehicle_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "MMM4M44");

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsAnyRentalForVehicleAsync(vehicle.Id);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsAnyRentalForDriverAsync_Should_Return_True_When_Any_Rental_For_Driver_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "NNN5N55");

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle.Id,
            startDate: new DateOnly(2025, 12, 1),
            expectedReturnDate: new DateOnly(2025, 12, 2));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rental);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsAnyRentalForDriverAsync(driver.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAnyRentalForDriverAsync_Should_Return_False_When_No_Rental_For_Driver_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Client client = CreateClient(companyId);
        Driver driver = CreateDriver(companyId, client.Id);

        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await rentalRepository.ExistsAnyRentalForDriverAsync(driver.Id);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task GetSummaryRowsByCompanyIdAsync_Should_Return_Rows_Ordered_By_StartDate_Desc()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();
        Guid otherCompanyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle1 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "OOO6O66", brand: "Ford", model: "Ka");
        Vehicle vehicle2 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "PPP7P77", brand: "Honda", model: "Civic");

        Client client = CreateClient(companyId, name: "Cliente A");
        Driver driver = CreateDriver(companyId, client.Id);

        Rental olderRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle1.Id,
            startDate: new DateOnly(2025, 1, 1),
            expectedReturnDate: new DateOnly(2025, 1, 2));

        Rental newerRental = CreateRental(
            companyId: companyId,
            clientId: client.Id,
            driverId: driver.Id,
            vehicleId: vehicle2.Id,
            startDate: new DateOnly(2025, 2, 1),
            expectedReturnDate: new DateOnly(2025, 2, 2));

        CompleteRental(
            rental: newerRental,
            actualReturnDate: new DateOnly(2025, 2, 2),
            grossRentalAmount: 1500m);

        VehicleGroup otherCompanyVehicleGroup = CreateVehicleGroup(otherCompanyId, "Grupo Outra Empresa");
        Vehicle otherCompanyVehicle = CreateVehicle(otherCompanyId, otherCompanyVehicleGroup.Id, licensePlate: "QQQ8Q88");

        Client otherCompanyClient = CreateClient(otherCompanyId, name: "Cliente Outra Empresa", email: "outro@teste.com");
        Driver otherCompanyDriver = CreateDriver(otherCompanyId, otherCompanyClient.Id, email: "outrocondutor@teste.com");

        Rental otherCompanyRental = CreateRental(
            companyId: otherCompanyId,
            clientId: otherCompanyClient.Id,
            driverId: otherCompanyDriver.Id,
            vehicleId: otherCompanyVehicle.Id,
            startDate: new DateOnly(2025, 12, 1),
            expectedReturnDate: new DateOnly(2025, 12, 2));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle1);
        dbContext.Vehicles.Add(vehicle2);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(olderRental);
        dbContext.Rentals.Add(newerRental);

        dbContext.VehicleGroups.Add(otherCompanyVehicleGroup);
        dbContext.Vehicles.Add(otherCompanyVehicle);
        dbContext.Clients.Add(otherCompanyClient);
        dbContext.Drivers.Add(otherCompanyDriver);
        dbContext.Rentals.Add(otherCompanyRental);

        await dbContext.SaveChangesAsync();

        // Act
        List<RentalSummaryRow> summaryRows = await rentalRepository.GetSummaryRowsByCompanyIdAsync(companyId, count: null, cancellationToken: CancellationToken.None);

        // Assert
        Assert.AreEqual(2, summaryRows.Count);

        DateOnly firstStartDate = ExtractStartDate(summaryRows[0]);
        DateOnly secondStartDate = ExtractStartDate(summaryRows[1]);

        Assert.IsTrue(firstStartDate > secondStartDate);

        Guid firstRentalId = ExtractSingleGuid(summaryRows[0]);
        Guid secondRentalId = ExtractSingleGuid(summaryRows[1]);

        Assert.AreEqual(newerRental.Id, firstRentalId);
        Assert.AreEqual(olderRental.Id, secondRentalId);

        IReadOnlyCollection<string> firstRowStrings = ExtractStringValues(summaryRows[0]);
        Assert.IsTrue(firstRowStrings.Contains("Cliente A"));
        Assert.IsTrue(firstRowStrings.Contains("Honda"));
        Assert.IsTrue(firstRowStrings.Contains("Civic"));
        Assert.IsTrue(firstRowStrings.Contains("PPP7P77"));

        IReadOnlyCollection<decimal> firstRowDecimals = ExtractDecimalValues(summaryRows[0]);
        Assert.IsTrue(firstRowDecimals.Contains(1500m));
        Assert.IsTrue(firstRowDecimals.Contains(500m));
    }

    [TestMethod]
    public async Task GetSummaryRowsByCompanyIdAsync_Should_Apply_Count_Limit_When_Count_Is_Positive()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle1 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "RRR9R99");
        Vehicle vehicle2 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "SSS1S11");
        Vehicle vehicle3 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "TTT2T22");

        Client client = CreateClient(companyId, name: "Cliente Count");
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rental1 = CreateRental(companyId, client.Id, driver.Id, vehicle1.Id, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2));
        Rental rental2 = CreateRental(companyId, client.Id, driver.Id, vehicle2.Id, new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 2));
        Rental rental3 = CreateRental(companyId, client.Id, driver.Id, vehicle3.Id, new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 2));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle1);
        dbContext.Vehicles.Add(vehicle2);
        dbContext.Vehicles.Add(vehicle3);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rental1);
        dbContext.Rentals.Add(rental2);
        dbContext.Rentals.Add(rental3);

        await dbContext.SaveChangesAsync();

        // Act
        List<RentalSummaryRow> summaryRows = await rentalRepository.GetSummaryRowsByCompanyIdAsync(companyId, count: 2, cancellationToken: CancellationToken.None);

        // Assert
        Assert.AreEqual(2, summaryRows.Count);

        Guid firstRentalId = ExtractSingleGuid(summaryRows[0]);
        Guid secondRentalId = ExtractSingleGuid(summaryRows[1]);

        Assert.AreEqual(rental3.Id, firstRentalId);
        Assert.AreEqual(rental2.Id, secondRentalId);
    }

    [TestMethod]
    public async Task GetSummaryRowsByCompanyIdAsync_Should_Not_Apply_Take_When_Count_Is_Zero()
    {
        // Arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryRental rentalRepository = _rentalRepository ?? throw new InvalidOperationException("Rental repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        Vehicle vehicle1 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "UUU3U33");
        Vehicle vehicle2 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "VVV4V44");
        Vehicle vehicle3 = CreateVehicle(companyId, vehicleGroup.Id, licensePlate: "WWW5W55");

        Client client = CreateClient(companyId, name: "Cliente Zero");
        Driver driver = CreateDriver(companyId, client.Id);

        Rental rental1 = CreateRental(companyId, client.Id, driver.Id, vehicle1.Id, new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2));
        Rental rental2 = CreateRental(companyId, client.Id, driver.Id, vehicle2.Id, new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 2));
        Rental rental3 = CreateRental(companyId, client.Id, driver.Id, vehicle3.Id, new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 2));

        dbContext.VehicleGroups.Add(vehicleGroup);
        dbContext.Vehicles.Add(vehicle1);
        dbContext.Vehicles.Add(vehicle2);
        dbContext.Vehicles.Add(vehicle3);
        dbContext.Clients.Add(client);
        dbContext.Drivers.Add(driver);
        dbContext.Rentals.Add(rental1);
        dbContext.Rentals.Add(rental2);
        dbContext.Rentals.Add(rental3);

        await dbContext.SaveChangesAsync();

        // Act
        List<RentalSummaryRow> summaryRows = await rentalRepository.GetSummaryRowsByCompanyIdAsync(companyId, count: 0, cancellationToken: CancellationToken.None);

        // Assert
        Assert.AreEqual(3, summaryRows.Count);
    }
}