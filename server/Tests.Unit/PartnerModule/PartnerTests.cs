using OblivionDrive.Domain.PartnerModule;

namespace OblivionDrive.Tests.Unit.PartnerModule;

[TestClass]
[TestCategory("Partner - Entity Unit Tests")]
public class PartnerTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string name = "NDD Partner";
        Guid companyId = Guid.NewGuid();

        // act
        Partner partner = new Partner(name, companyId);

        // assert
        Assert.AreNotEqual(Guid.Empty, partner.Id);
        Assert.AreEqual(companyId, partner.CompanyId);

        Assert.AreEqual(name, partner.Name);

        Assert.IsNotNull(partner.Coupons);
        Assert.AreEqual(0, partner.Coupons.Count);
    }

    [TestMethod]
    public void Update_Should_Update_Name_And_Keep_Id_And_CompanyId()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Partner originalPartner = new Partner(
            name: "Original partner",
            companyId: companyId);

        Guid originalId = originalPartner.Id;
        Guid originalCompanyId = originalPartner.CompanyId;

        Partner updatedPartner = new Partner(
            name: "Updated partner",
            companyId: Guid.NewGuid());

        // act
        originalPartner.Update(updatedPartner);

        // assert
        Assert.AreEqual(updatedPartner.Name, originalPartner.Name);

        Assert.AreEqual(originalId, originalPartner.Id);
        Assert.AreEqual(originalCompanyId, originalPartner.CompanyId);
    }
}