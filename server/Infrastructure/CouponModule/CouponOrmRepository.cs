
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.CouponModule;
public class CouponOrmRepository(OblivionDriveDbContext context) : BaseRepository<Coupon>(context), IRepositoryCoupon
{
    public async Task<bool> ExistsByNameAsync(string couponName)
    {
        return await context.Coupons
            .AnyAsync(c => c.Name == couponName);
    }

    public async Task<bool> ExistsByNameAsync(string couponName, Guid couponIdToIgnore)
    {
        return await context.Coupons
            .AnyAsync(c => c.Name == couponName && c.Id != couponIdToIgnore);
    }

    public async Task<Coupon?> GetByNameAsync(string couponName)
    {
        return await context.Coupons
            .FirstOrDefaultAsync(c => c.Name == couponName);
    }
}