namespace OblivionDrive.Domain.Shared;

public interface IRepository<T> where T : BaseEntity<T>
{
    Task<Guid> AddAsync(T newEntity);
    Task AddRangeAsync(IList<T> entities);
    Task<T> UpdateAsync(Guid id, T updatedEntity);
    Task<bool> DeleteAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> GetAllAsync(int count);
    Task<T?> GetByIdAsync(Guid id);
}