using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public class BaseRepository<T> where T : BaseEntity<T>
{
    protected readonly OblivionDriveDbContext DbContext;
    protected readonly DbSet<T> Records;

    public BaseRepository(OblivionDriveDbContext dbContext)
    {
        DbContext = dbContext;
        Records = dbContext.Set<T>();
    }

    public async Task<Guid> AddAsync(T newEntity)
    {
        await Records.AddAsync(newEntity);

        return newEntity.Id;
    }

    public Task AddRangeAsync(IList<T> entities)
    {
        return Records.AddRangeAsync(entities);
    }

    public async Task<T> UpdateAsync(Guid id, T updatedEntity)
    {
        var existingEntity = await GetByIdAsync(id);

        existingEntity!.Update(updatedEntity);

        return existingEntity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existingEntity = await GetByIdAsync(id);

        Records.Remove(existingEntity!);

        return true;
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        return await Records.ToListAsync();
    }

    public virtual async Task<List<T>> GetAllAsync(int count)
    {
        return await Records.Take(count).ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await Records.SingleOrDefaultAsync(x => x.Id == id);
    }
}
