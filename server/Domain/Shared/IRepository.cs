namespace OblivionDrive.Domain.Shared;

public interface IRepository<T> where T : BaseEntity<T>
{
    Task<Guid> AddAsync(T newEntity);
    Task AddRangeAsync(IList<T> entities);
    Task<T> UpdateAsync(T entity, T updatedEntity);
    Task<bool> DeleteAsync(T Entity);
    Task<List<T>> GetAllAsync();
    Task<List<T>> GetAllAsync(int count);
    Task<T?> GetByIdAsync(Guid id);
}