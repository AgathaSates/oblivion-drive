using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule;

[TestClass]
[TestCategory("VehicleGroup - Entity Unit Tests")]
public class VehicleGroupTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string name = "SUV Premium";
        Guid companyId = Guid.NewGuid();

        // act
        VehicleGroup vehicleGroup = new VehicleGroup(name, companyId);

        // assert
        Assert.AreNotEqual(Guid.Empty, vehicleGroup.Id);
        Assert.AreEqual(companyId, vehicleGroup.CompanyId);

        Assert.AreEqual(name, vehicleGroup.Name);
        Assert.IsNotNull(vehicleGroup.BillingPlans);
        Assert.AreEqual(0, vehicleGroup.BillingPlans.Count);
    }

    [TestMethod]
    public void Update_Should_Update_Name_And_Keep_Id_CompanyId_And_BillingPlans_Reference()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        VehicleGroup originalVehicleGroup = new VehicleGroup(
            name: "Grupo original",
            companyId: companyId);

        Guid originalId = originalVehicleGroup.Id;
        Guid originalCompanyId = originalVehicleGroup.CompanyId;
        ICollection<BillingPlan> originalBillingPlansReference = originalVehicleGroup.BillingPlans;

        VehicleGroup updatedVehicleGroup = new VehicleGroup(
            name: "Grupo atualizado",
            companyId: Guid.NewGuid());

        // act
        originalVehicleGroup.Update(updatedVehicleGroup);

        // assert
        Assert.AreEqual(updatedVehicleGroup.Name, originalVehicleGroup.Name);

        Assert.AreEqual(originalId, originalVehicleGroup.Id);
        Assert.AreEqual(originalCompanyId, originalVehicleGroup.CompanyId);

        Assert.AreSame(originalBillingPlansReference, originalVehicleGroup.BillingPlans);
    }
}