using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.CouponModule;
public interface IRepositoryCoupon : IRepository<Coupon>
{
    Task<bool> ExistsByNameAsync(string couponName);
    Task<bool> ExistsByNameAsync(string couponName, Guid couponIdToIgnore);
}
