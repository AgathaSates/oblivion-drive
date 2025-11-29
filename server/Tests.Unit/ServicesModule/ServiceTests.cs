using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Tests.Unit.ServicesModule;

[TestClass]
[TestCategory("Service - Entity Unit Tests")]
public class ServiceTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string name = "Wash and vacuum";
        decimal price = 99.90m;
        ChargeType chargeType = (ChargeType)1;
        Guid companyId = Guid.NewGuid();

        // act
        Service service = new Service(name, price, chargeType, companyId);

        // assert
        Assert.AreNotEqual(Guid.Empty, service.Id);
        Assert.AreEqual(companyId, service.CompanyId);

        Assert.AreEqual(name, service.Name);
        Assert.AreEqual(price, service.Price);
        Assert.AreEqual(chargeType, service.ChargeType);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_And_CompanyId()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Service originalService = new Service(
            name: "Original service",
            price: 50.00m,
            chargeType: (ChargeType)1,
            companyId: companyId);

        Guid originalId = originalService.Id;
        Guid originalCompanyId = originalService.CompanyId;

        Service updatedService = new Service(
            name: "Updated service",
            price: 75.00m,
            chargeType: (ChargeType)2,
            companyId: Guid.NewGuid());

        // act
        originalService.Update(updatedService);

        // assert
        Assert.AreEqual(updatedService.Name, originalService.Name);
        Assert.AreEqual(updatedService.Price, originalService.Price);
        Assert.AreEqual(updatedService.ChargeType, originalService.ChargeType);

        Assert.AreEqual(originalId, originalService.Id);
        Assert.AreEqual(originalCompanyId, originalService.CompanyId);
    }
}