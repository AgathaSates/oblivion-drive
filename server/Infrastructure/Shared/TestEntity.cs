using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public class TestEntity : BaseEntity<TestEntity>
{
    public string Name { get; private set; }

    public TestEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public override void Update(TestEntity updatedEntity)
    {
        Name = updatedEntity.Name;
    }
}
