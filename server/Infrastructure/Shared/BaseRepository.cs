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

    public Task<T> UpdateAsync(T entity, T updatedEntity)
    {
        entity!.Update(updatedEntity);

        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(T Entity)
    {
        Records.Remove(Entity);

        return Task.FromResult(true);
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
