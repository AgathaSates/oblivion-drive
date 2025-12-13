using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Tests.Unit.RentalModule;
[TestClass]
[TestCategory("Rental - Entity Unit Tests")]
public class RentalTests
{
    private static Rental CreateRental(
        Guid companyId,
        Guid clientId,
        Guid driverId,
        Guid vehicleId,
        RentalPlanType planType,
        DateOnly startDate,
        DateOnly expectedReturnDate,
        decimal insuranceDailyPricePerPerson,
        int insurancePersonsCount,
        int estimatedTotalKilometers,
        decimal servicesTotalPrice,
        decimal insuranceTotalPrice,
        decimal rentalBasePrice,
        decimal estimatedRentalAmount,
        IEnumerable<Guid>? serviceIds = null)
    {
        return new Rental(
            companyId: companyId,
            clientId: clientId,
            driverId: driverId,
            vehicleId: vehicleId,
            planType: planType,
            startDate: startDate,
            expectedReturnDate: expectedReturnDate,
            insuranceDailyPricePerPerson: insuranceDailyPricePerPerson,
            insurancePersonsCount: insurancePersonsCount,
            estimatedTotalKilometers: estimatedTotalKilometers,
            servicesTotalPrice: servicesTotalPrice,
            insuranceTotalPrice: insuranceTotalPrice,
            rentalBasePrice: rentalBasePrice,
            estimatedRentalAmount: estimatedRentalAmount,
            serviceIds: serviceIds
        );
    }

    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties_And_Defaults()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        Guid clientId = Guid.NewGuid();
        Guid driverId = Guid.NewGuid();
        Guid vehicleId = Guid.NewGuid();

        RentalPlanType planType = (RentalPlanType)1;

        DateOnly startDate = new(2025, 01, 10);
        DateOnly expectedReturnDate = new(2025, 01, 15);

        decimal insuranceDailyPricePerPerson = 15m;
        int insurancePersonsCount = 2;

        int estimatedTotalKilometers = 300;

        decimal servicesTotalPrice = 100m;
        decimal insuranceTotalPrice = 150m;
        decimal rentalBasePrice = 500m;
        decimal estimatedRentalAmount = 750m;

        List<Guid> serviceIds = [Guid.NewGuid(), Guid.NewGuid()];

        // act
        Rental rental = CreateRental(
            companyId,
            clientId,
            driverId,
            vehicleId,
            planType,
            startDate,
            expectedReturnDate,
            insuranceDailyPricePerPerson,
            insurancePersonsCount,
            estimatedTotalKilometers,
            servicesTotalPrice,
            insuranceTotalPrice,
            rentalBasePrice,
            estimatedRentalAmount,
            serviceIds
        );

        // assert
        Assert.AreNotEqual(Guid.Empty, rental.Id);
        Assert.AreEqual(companyId, rental.CompanyId);

        Assert.AreEqual(clientId, rental.ClientId);
        Assert.AreEqual(driverId, rental.DriverId);
        Assert.AreEqual(vehicleId, rental.VehicleId);

        Assert.AreEqual(planType, rental.PlanType);

        Assert.AreEqual(startDate, rental.StartDate);
        Assert.AreEqual(expectedReturnDate, rental.ExpectedReturnDate);
        Assert.IsNull(rental.ActualReturnDate);

        Assert.AreEqual(insuranceDailyPricePerPerson, rental.InsuranceDailyPricePerPerson);
        Assert.AreEqual(insurancePersonsCount, rental.InsurancePersonsCount);

        Assert.AreEqual(Rental.DefaultSecurityDepositAmount, rental.SecurityDepositAmount);

        Assert.IsFalse(rental.IsCompleted);

        Assert.AreEqual(estimatedTotalKilometers, rental.EstimatedTotalKilometers);

        Assert.AreEqual(servicesTotalPrice, rental.ServicesTotalPrice);
        Assert.AreEqual(insuranceTotalPrice, rental.InsuranceTotalPrice);
        Assert.AreEqual(rentalBasePrice, rental.RentalBasePrice);
        Assert.AreEqual(estimatedRentalAmount, rental.EstimatedRentalAmount);

        CollectionAssert.AreEquivalent(serviceIds, rental.ServiceIds.ToList());

        Assert.AreEqual(0, rental.InitialOdometerInKm);
        Assert.IsNull(rental.CurrentOdometerInKm);
        Assert.IsFalse(rental.HasDamage);
        Assert.IsFalse(rental.IsFuelTankFullOnReturn);

        Assert.IsNull(rental.CouponId);
        Assert.AreEqual(0m, rental.CouponDiscountAmount);
    }

    [TestMethod]
    public void Constructor_Should_Initialize_ServiceIds_As_Empty_When_Null()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        // act
        Rental rental = CreateRental(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)1,
            startDate: new DateOnly(2025, 01, 10),
            expectedReturnDate: new DateOnly(2025, 01, 15),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m,
            serviceIds: null
        );

        // assert
        Assert.IsNotNull(rental.ServiceIds);
        Assert.AreEqual(0, rental.ServiceIds.Count);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_And_CompanyId()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Rental originalRental = CreateRental(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)1,
            startDate: new DateOnly(2025, 01, 10),
            expectedReturnDate: new DateOnly(2025, 01, 15),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 10m,
            insuranceTotalPrice: 20m,
            rentalBasePrice: 30m,
            estimatedRentalAmount: 60m,
            serviceIds: [Guid.NewGuid()]
        );

        Guid originalId = originalRental.Id;
        Guid originalCompanyId = originalRental.CompanyId;

        List<Guid> updatedServiceIds = [Guid.NewGuid(), Guid.NewGuid()];

        Rental updatedRental = CreateRental(
            companyId: Guid.NewGuid(), // não deve “vazar” pro original
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)2,
            startDate: new DateOnly(2025, 02, 01),
            expectedReturnDate: new DateOnly(2025, 02, 05),
            insuranceDailyPricePerPerson: 25m,
            insurancePersonsCount: 3,
            estimatedTotalKilometers: 500,
            servicesTotalPrice: 111m,
            insuranceTotalPrice: 222m,
            rentalBasePrice: 333m,
            estimatedRentalAmount: 666m,
            serviceIds: updatedServiceIds
        );

        // act
        originalRental.Update(updatedRental);

        // assert
        Assert.AreEqual(updatedRental.ClientId, originalRental.ClientId);
        Assert.AreEqual(updatedRental.DriverId, originalRental.DriverId);
        Assert.AreEqual(updatedRental.VehicleId, originalRental.VehicleId);

        Assert.AreEqual(updatedRental.PlanType, originalRental.PlanType);
        Assert.AreEqual(updatedRental.StartDate, originalRental.StartDate);
        Assert.AreEqual(updatedRental.ExpectedReturnDate, originalRental.ExpectedReturnDate);

        Assert.AreEqual(updatedRental.InsuranceDailyPricePerPerson, originalRental.InsuranceDailyPricePerPerson);
        Assert.AreEqual(updatedRental.InsurancePersonsCount, originalRental.InsurancePersonsCount);

        Assert.AreEqual(updatedRental.EstimatedTotalKilometers, originalRental.EstimatedTotalKilometers);

        Assert.AreEqual(updatedRental.ServicesTotalPrice, originalRental.ServicesTotalPrice);
        Assert.AreEqual(updatedRental.InsuranceTotalPrice, originalRental.InsuranceTotalPrice);
        Assert.AreEqual(updatedRental.RentalBasePrice, originalRental.RentalBasePrice);
        Assert.AreEqual(updatedRental.EstimatedRentalAmount, originalRental.EstimatedRentalAmount);

        CollectionAssert.AreEquivalent(updatedServiceIds, originalRental.ServiceIds.ToList());

        Assert.AreEqual(originalId, originalRental.Id);
        Assert.AreEqual(originalCompanyId, originalRental.CompanyId);
    }

    [TestMethod]
    public void CompleteReturn_Should_Set_FinalAmountToPay_To_Gross_When_HasDamage()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        Rental rental = CreateRental(
            companyId: companyId,
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)1,
            startDate: new DateOnly(2025, 01, 10),
            expectedReturnDate: new DateOnly(2025, 01, 15),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m
        );

        DateOnly actualReturnDate = new(2025, 01, 15);
        int initialOdometerInKm = 10_000;
        int currentOdometerInKm = 10_150;

        decimal grossRentalAmount = 1200m;

        // act
        rental.CompleteReturn(
            actualReturnDate: actualReturnDate,
            initialOdometerInKm: initialOdometerInKm,
            currentOdometerInKm: currentOdometerInKm,
            isFuelTankFullOnReturn: true,
            hasDamage: true,
            rentalBasePrice: 400m,
            insuranceTotalPrice: 100m,
            servicesTotalPrice: 50m,
            fuelChargePrice: 0m,
            penaltyPrice: 0m,
            grossRentalAmount: grossRentalAmount,
            couponId: null,
            couponDiscountAmount: 0m
        );

        // assert
        Assert.IsTrue(rental.IsCompleted);
        Assert.AreEqual(actualReturnDate, rental.ActualReturnDate);

        Assert.AreEqual(initialOdometerInKm, rental.InitialOdometerInKm);
        Assert.AreEqual(currentOdometerInKm, rental.CurrentOdometerInKm);

        Assert.IsTrue(rental.IsFuelTankFullOnReturn);
        Assert.IsTrue(rental.HasDamage);

        Assert.AreEqual(grossRentalAmount, rental.GrossRentalAmount);
        Assert.AreEqual(grossRentalAmount, rental.FinalAmountToPay);
    }

    [TestMethod]
    public void CompleteReturn_Should_Subtract_Deposit_When_No_Damage_And_Gross_Greater_Than_Deposit()
    {
        // arrange
        Rental rental = CreateRental(
            companyId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)1,
            startDate: new DateOnly(2025, 01, 10),
            expectedReturnDate: new DateOnly(2025, 01, 15),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m
        );

        decimal grossRentalAmount = 1500m;
        decimal expectedFinalAmountToPay = grossRentalAmount - Rental.DefaultSecurityDepositAmount; // 500

        // act
        rental.CompleteReturn(
            actualReturnDate: new DateOnly(2025, 01, 15),
            initialOdometerInKm: 10_000,
            currentOdometerInKm: 10_150,
            isFuelTankFullOnReturn: true,
            hasDamage: false,
            rentalBasePrice: 0m,
            insuranceTotalPrice: 0m,
            servicesTotalPrice: 0m,
            fuelChargePrice: 0m,
            penaltyPrice: 0m,
            grossRentalAmount: grossRentalAmount,
            couponId: null,
            couponDiscountAmount: 0m
        );

        // assert
        Assert.IsTrue(rental.IsCompleted);
        Assert.AreEqual(grossRentalAmount, rental.GrossRentalAmount);
        Assert.AreEqual(expectedFinalAmountToPay, rental.FinalAmountToPay);
    }

    [TestMethod]
    public void CompleteReturn_Should_Set_FinalAmountToPay_To_Zero_When_No_Damage_And_Gross_Less_Or_Equal_Deposit()
    {
        // arrange
        Rental rental = CreateRental(
            companyId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)1,
            startDate: new DateOnly(2025, 01, 10),
            expectedReturnDate: new DateOnly(2025, 01, 15),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m
        );

        decimal grossRentalAmount = 1000m; // igual à caução

        // act
        rental.CompleteReturn(
            actualReturnDate: new DateOnly(2025, 01, 15),
            initialOdometerInKm: 10_000,
            currentOdometerInKm: 10_150,
            isFuelTankFullOnReturn: true,
            hasDamage: false,
            rentalBasePrice: 0m,
            insuranceTotalPrice: 0m,
            servicesTotalPrice: 0m,
            fuelChargePrice: 0m,
            penaltyPrice: 0m,
            grossRentalAmount: grossRentalAmount,
            couponId: null,
            couponDiscountAmount: 0m
        );

        // assert
        Assert.IsTrue(rental.IsCompleted);
        Assert.AreEqual(0m, rental.FinalAmountToPay);
    }

    [TestMethod]
    public void CompleteReturn_Should_Set_Coupon_Fields_And_Apply_Discount_When_Discount_Is_Provided()
    {
        // arrange
        Rental rental = CreateRental(
            companyId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            planType: (RentalPlanType)1,
            startDate: new DateOnly(2025, 01, 10),
            expectedReturnDate: new DateOnly(2025, 01, 15),
            insuranceDailyPricePerPerson: 10m,
            insurancePersonsCount: 1,
            estimatedTotalKilometers: 100,
            servicesTotalPrice: 0m,
            insuranceTotalPrice: 0m,
            rentalBasePrice: 0m,
            estimatedRentalAmount: 0m
        );

        Guid couponId = Guid.NewGuid();
        decimal couponDiscountAmount = 200m;

        decimal grossRentalAmount = 1500m;
        decimal expectedAfterDeposit = grossRentalAmount - Rental.DefaultSecurityDepositAmount; // 500
        decimal expectedFinal = expectedAfterDeposit - couponDiscountAmount; // 300

        // act
        rental.CompleteReturn(
            actualReturnDate: new DateOnly(2025, 01, 15),
            initialOdometerInKm: 10_000,
            currentOdometerInKm: 10_150,
            isFuelTankFullOnReturn: true,
            hasDamage: false,
            rentalBasePrice: 0m,
            insuranceTotalPrice: 0m,
            servicesTotalPrice: 0m,
            fuelChargePrice: 0m,
            penaltyPrice: 0m,
            grossRentalAmount: grossRentalAmount,
            couponId: couponId,
            couponDiscountAmount: couponDiscountAmount
        );

        // assert
        Assert.AreEqual(couponId, rental.CouponId);
        Assert.AreEqual(couponDiscountAmount, rental.CouponDiscountAmount);
        Assert.AreEqual(expectedFinal, rental.FinalAmountToPay);
    }
}
