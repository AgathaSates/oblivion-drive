using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;

namespace OblivionDrive.Tests.Unit.CouponModule;

[TestClass]
[TestCategory("Coupon - Coupon Entity Unit Tests")]
public sealed class CouponTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string couponName = "CUPOM10";
        decimal couponValue = 50.00m;
        DateOnly expirationDate = new DateOnly(2024, 12, 31);
        Guid partnerId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        // act
        Coupon coupon = new Coupon(
            couponName,
            couponValue,
            expirationDate,
            partnerId,
            companyId);

        // assert
        Assert.IsNotNull(coupon.Id);
        Assert.AreNotEqual(Guid.Empty, coupon.Id);
        Assert.AreEqual(companyId, coupon.CompanyId);
        Assert.AreEqual(couponName, coupon.Name);
        Assert.AreEqual(couponValue, coupon.Value);
        Assert.AreEqual(expirationDate, coupon.ExpirationDate);
        Assert.AreEqual(partnerId, coupon.PartnerId);
        Assert.IsNotNull(coupon.UsedByClientIds);
        Assert.AreEqual(0, coupon.UsedByClientIds.Count);
    }

    [TestMethod]
    public void Update_Should_Update_Name_Value_ExpirationDate_And_PartnerId()
    {
        // arrange
        string originalName = "CUPOM10";
        decimal originalValue = 50.00m;
        DateOnly originalExpirationDate = new DateOnly(2024, 12, 31);
        Guid originalPartnerId = Guid.NewGuid();
        Guid originalCompanyId = Guid.NewGuid();

        Coupon coupon = new Coupon(
            originalName,
            originalValue,
            originalExpirationDate,
            originalPartnerId,
            originalCompanyId);

        Guid originalCouponId = coupon.Id;

        string updatedName = "CUPOM20";
        decimal updatedValue = 100.00m;
        DateOnly updatedExpirationDate = new DateOnly(2025, 6, 30);
        Guid updatedPartnerId = Guid.NewGuid();
        Guid updatedCompanyId = Guid.NewGuid();

        Coupon updatedCoupon = new Coupon(
            updatedName,
            updatedValue,
            updatedExpirationDate,
            updatedPartnerId,
            updatedCompanyId);

        // act
        coupon.Update(updatedCoupon);

        // assert
        Assert.AreEqual(updatedName, coupon.Name);
        Assert.AreEqual(updatedValue, coupon.Value);
        Assert.AreEqual(updatedExpirationDate, coupon.ExpirationDate);
        Assert.AreEqual(updatedPartnerId, coupon.PartnerId);

        // ID e CompanyId não devem ser alterados
        Assert.AreEqual(originalCouponId, coupon.Id);
        Assert.AreEqual(originalCompanyId, coupon.CompanyId);
    }

    [TestMethod]
    public void IsExpired_Should_Return_True_When_ExpirationDate_Is_Before_Today()
    {
        // arrange
        DateOnly expirationDate = new DateOnly(2024, 1, 1);
        DateOnly today = new DateOnly(2024, 6, 1);

        Coupon coupon = new Coupon(
            "CUPOM_EXPIRADO",
            50.00m,
            expirationDate,
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        bool isExpired = coupon.IsExpired(today);

        // assert
        Assert.IsTrue(isExpired);
    }

    [TestMethod]
    public void IsExpired_Should_Return_False_When_ExpirationDate_Is_After_Today()
    {
        // arrange
        DateOnly expirationDate = new DateOnly(2024, 12, 31);
        DateOnly today = new DateOnly(2024, 6, 1);

        Coupon coupon = new Coupon(
            "CUPOM_VALIDO",
            50.00m,
            expirationDate,
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        bool isExpired = coupon.IsExpired(today);

        // assert
        Assert.IsFalse(isExpired);
    }

    [TestMethod]
    public void IsExpired_Should_Return_False_When_ExpirationDate_Is_Equal_To_Today()
    {
        // arrange
        DateOnly expirationDate = new DateOnly(2024, 6, 1);
        DateOnly today = new DateOnly(2024, 6, 1);

        Coupon coupon = new Coupon(
            "CUPOM_HOJE",
            50.00m,
            expirationDate,
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        bool isExpired = coupon.IsExpired(today);

        // assert
        Assert.IsFalse(isExpired);
    }

    [TestMethod]
    public void HasAlreadyBeenUsedBy_Should_Return_False_When_Client_Has_Not_Used_Coupon()
    {
        // arrange
        Guid clientId = Guid.NewGuid();

        Coupon coupon = new Coupon(
            "CUPOM10",
            50.00m,
            new DateOnly(2024, 12, 31),
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        bool hasBeenUsed = coupon.HasAlreadyBeenUsedBy(clientId);

        // assert
        Assert.IsFalse(hasBeenUsed);
    }

    [TestMethod]
    public void TryMarkAsUsedBy_Should_Return_True_And_Add_ClientId_When_First_Use()
    {
        // arrange
        Guid clientId = Guid.NewGuid();

        Coupon coupon = new Coupon(
            "CUPOM10",
            50.00m,
            new DateOnly(2024, 12, 31),
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        bool result = coupon.TryMarkAsUsedBy(clientId);

        // assert
        Assert.IsTrue(result);
        Assert.IsTrue(coupon.HasAlreadyBeenUsedBy(clientId));
        Assert.AreEqual(1, coupon.UsedByClientIds.Count);
        Assert.IsTrue(coupon.UsedByClientIds.Contains(clientId));
    }

    [TestMethod]
    public void TryMarkAsUsedBy_Should_Return_False_When_Client_Already_Used_Coupon()
    {
        // arrange
        Guid clientId = Guid.NewGuid();

        Coupon coupon = new Coupon(
            "CUPOM10",
            50.00m,
            new DateOnly(2024, 12, 31),
            Guid.NewGuid(),
            Guid.NewGuid());

        coupon.TryMarkAsUsedBy(clientId);

        // act
        bool result = coupon.TryMarkAsUsedBy(clientId);

        // assert
        Assert.IsFalse(result);
        Assert.IsTrue(coupon.HasAlreadyBeenUsedBy(clientId));
        Assert.AreEqual(1, coupon.UsedByClientIds.Count);
    }

    [TestMethod]
    public void TryMarkAsUsedBy_Should_Allow_Multiple_Different_Clients()
    {
        // arrange
        Guid clientId1 = Guid.NewGuid();
        Guid clientId2 = Guid.NewGuid();
        Guid clientId3 = Guid.NewGuid();

        Coupon coupon = new Coupon(
            "CUPOM10",
            50.00m,
            new DateOnly(2024, 12, 31),
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        bool result1 = coupon.TryMarkAsUsedBy(clientId1);
        bool result2 = coupon.TryMarkAsUsedBy(clientId2);
        bool result3 = coupon.TryMarkAsUsedBy(clientId3);

        // assert
        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.IsTrue(result3);

        Assert.AreEqual(3, coupon.UsedByClientIds.Count);
        Assert.IsTrue(coupon.HasAlreadyBeenUsedBy(clientId1));
        Assert.IsTrue(coupon.HasAlreadyBeenUsedBy(clientId2));
        Assert.IsTrue(coupon.HasAlreadyBeenUsedBy(clientId3));
    }

    [TestMethod]
    public void UsedByClientIds_Should_Be_ReadOnly_Collection()
    {
        // arrange
        Coupon coupon = new Coupon(
            "CUPOM10",
            50.00m,
            new DateOnly(2024, 12, 31),
            Guid.NewGuid(),
            Guid.NewGuid());

        // act
        var usedByClientIds = coupon.UsedByClientIds;

        // assert
        Assert.IsNotNull(usedByClientIds);
        Assert.IsInstanceOfType<IReadOnlyCollection<Guid>>(usedByClientIds);
    }
}
