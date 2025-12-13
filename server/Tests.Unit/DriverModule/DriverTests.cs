
using OblivionDrive.Domain.DriverModule;

namespace OblivionDrive.Tests.Unit.DriverModule;
[TestClass]
[TestCategory("Driver - Entity Unit Tests")]
public class DriverTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();

        string name = "John Driver";
        string email = "john.driver@email.com";
        string phoneNumber = "(47) 99999-9999";

        string cpf = "12345678901";
        string cnh = "CNH123456";
        DateOnly cnhExpirationDate = new DateOnly(2030, 12, 31);

        bool isClientAlsoDriver = true;

        // act
        Driver driver = new Driver(
            companyId: companyId,
            clientId: clientId,
            name: name,
            phoneNumber: phoneNumber,
            cpf: cpf,
            cnh: cnh,
            cnhExpirationDate: cnhExpirationDate,
            email: email,
            isClientAlsoDriver: isClientAlsoDriver);

        // assert
        Assert.AreNotEqual(Guid.Empty, driver.Id);
        Assert.AreEqual(companyId, driver.CompanyId);

        Assert.AreEqual(clientId, driver.ClientId);

        Assert.AreEqual(name, driver.Name);
        Assert.AreEqual(email, driver.Email);
        Assert.AreEqual(phoneNumber, driver.PhoneNumber);

        Assert.AreEqual(cpf, driver.Cpf);
        Assert.AreEqual(cnh, driver.Cnh);
        Assert.AreEqual(cnhExpirationDate, driver.CnhExpirationDate);

        Assert.AreEqual(isClientAlsoDriver, driver.IsClientAlsoDriver);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_And_CompanyId()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Driver originalDriver = new Driver(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            name: "Original Name",
            phoneNumber: "(47) 90000-0000",
            cpf: "11111111111",
            cnh: "CNH-ORIGINAL",
            cnhExpirationDate: new DateOnly(2028, 1, 1),
            email: "original@email.com",
            isClientAlsoDriver: false);

        Guid originalId = originalDriver.Id;
        Guid originalCompanyId = originalDriver.CompanyId;

        Driver updatedDriver = new Driver(
            companyId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            name: "Updated Name",
            phoneNumber: "(47) 98888-8888",
            cpf: "22222222222",
            cnh: "CNH-UPDATED",
            cnhExpirationDate: new DateOnly(2032, 6, 15),
            email: "updated@email.com",
            isClientAlsoDriver: true);

        // act
        originalDriver.Update(updatedDriver);

        // assert
        Assert.AreEqual(updatedDriver.Name, originalDriver.Name);
        Assert.AreEqual(updatedDriver.Email, originalDriver.Email);
        Assert.AreEqual(updatedDriver.PhoneNumber, originalDriver.PhoneNumber);

        Assert.AreEqual(updatedDriver.Cpf, originalDriver.Cpf);
        Assert.AreEqual(updatedDriver.Cnh, originalDriver.Cnh);
        Assert.AreEqual(updatedDriver.CnhExpirationDate, originalDriver.CnhExpirationDate);

        Assert.AreEqual(updatedDriver.ClientId, originalDriver.ClientId);
        Assert.AreEqual(updatedDriver.IsClientAlsoDriver, originalDriver.IsClientAlsoDriver);

        Assert.AreEqual(originalId, originalDriver.Id);
        Assert.AreEqual(originalCompanyId, originalDriver.CompanyId);
    }

    [TestMethod]
    public void Constructor_Should_Default_IsClientAlsoDriver_To_False_When_Not_Provided()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();

        // act
        Driver driver = new Driver(
            companyId: companyId,
            clientId: clientId,
            name: "Driver",
            phoneNumber: "(47) 97777-7777",
            cpf: "33333333333",
            cnh: "CNH-DEFAULT",
            cnhExpirationDate: new DateOnly(2031, 1, 1),
            email: "driver@email.com");

        // assert
        Assert.IsFalse(driver.IsClientAlsoDriver);
    }
}