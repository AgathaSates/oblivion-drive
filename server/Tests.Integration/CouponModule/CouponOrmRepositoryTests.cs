using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.CouponModule;

[TestClass]
[TestCategory("CouponOrmRepository Infrastructure - Integration Tests")]
public class CouponOrmRepositoryTests : TestFixture
{
    private static Partner CreatePartner(Guid companyId, string name = "Parceiro Teste")
    {
        return new Partner(
            name: name,
            companyId: companyId);
    }

    private static Coupon CreateCoupon(
        Guid companyId,
        Guid partnerId,
        string name,
        decimal value = 25m,
        DateOnly? expirationDate = null)
    {
        DateOnly effectiveExpirationDate =
            expirationDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        return new Coupon(
            name: name,
            value: value,
            expirationDate: effectiveExpirationDate,
            partnerId: partnerId,
            companyId: companyId);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_True_When_Coupon_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string couponName = "CUPOM10";

        Partner partner = CreatePartner(companyId);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        Coupon coupon = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: couponName);

        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await couponRepository.ExistsByNameAsync(couponName);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Coupon_With_Name_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Partner partner = CreatePartner(companyId);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        Coupon existingCoupon = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: "CUPOM-EXISTENTE");

        dbContext.Coupons.Add(existingCoupon);
        await dbContext.SaveChangesAsync();

        string searchedName = "CUPOM-INEXISTENTE";

        // act
        bool exists = await couponRepository.ExistsByNameAsync(searchedName);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        // act
        bool existsForEmpty = await couponRepository.ExistsByNameAsync(string.Empty);
        bool existsForWhitespace = await couponRepository.ExistsByNameAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_False_When_Only_Coupon_With_Name_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string couponName = "CUPOM-UNICO";

        Partner partner = CreatePartner(companyId);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        Coupon coupon = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: couponName);

        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await couponRepository.ExistsByNameAsync(couponName, coupon.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio cupom como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_True_When_Other_Coupon_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedCouponName = "CUPOM-DUPLICADO";

        Partner partner = CreatePartner(companyId);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        Coupon coupon1 = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: duplicatedCouponName);

        Coupon coupon2 = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: duplicatedCouponName);

        dbContext.Coupons.Add(coupon1);
        dbContext.Coupons.Add(coupon2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await couponRepository.ExistsByNameAsync(duplicatedCouponName, coupon1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro cupom com o mesmo nome.");
    }

    [TestMethod]
    public async Task GetByNameAsync_Should_Return_Coupon_When_Coupon_With_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string couponName = "CUPOM-GET";

        Partner partner = CreatePartner(companyId);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        Coupon coupon = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: couponName,
            value: 50m,
            expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));

        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync();

        // act
        Coupon? result = await couponRepository.GetByNameAsync(couponName);

        // assert
        Assert.IsNotNull(result, "Deveria retornar um cupom quando existir cupom com o nome informado.");
        Assert.AreEqual(coupon.Id, result!.Id);
        Assert.AreEqual(couponName, result.Name);
        Assert.AreEqual(coupon.Value, result.Value);
        Assert.AreEqual(coupon.ExpirationDate, result.ExpirationDate);
        Assert.AreEqual(partner.Id, result.PartnerId);
    }

    [TestMethod]
    public async Task GetByNameAsync_Should_Return_Null_When_Coupon_With_Name_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Partner partner = CreatePartner(companyId);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        Coupon existingCoupon = CreateCoupon(
            companyId: companyId,
            partnerId: partner.Id,
            name: "CUPOM-EXISTE");

        dbContext.Coupons.Add(existingCoupon);
        await dbContext.SaveChangesAsync();

        string searchedName = "CUPOM-NAO-EXISTE";

        // act
        Coupon? result = await couponRepository.GetByNameAsync(searchedName);

        // assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByNameAsync_Should_Return_Null_When_Name_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryCoupon couponRepository =
            _couponRepository ?? throw new InvalidOperationException("Coupon repository not initialized.");

        // act
        Coupon? resultForEmpty = await couponRepository.GetByNameAsync(string.Empty);
        Coupon? resultForWhitespace = await couponRepository.GetByNameAsync("   ");

        // assert
        Assert.IsNull(resultForEmpty);
        Assert.IsNull(resultForWhitespace);
    }
}