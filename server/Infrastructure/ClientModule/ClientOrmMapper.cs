using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Infrastructure.Orm.ClientModule;
public class ClientOrmMapper : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(client => client.Id);

        builder.Property(client => client.CompanyId)
            .IsRequired();

        builder.Property(client => client.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(client => client.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(client => client.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(client => client.ClientType)
            .IsRequired();

        builder.Property(client => client.Cpf)
            .HasMaxLength(14);

        builder.Property(client => client.Rg)
            .HasMaxLength(20);

        builder.Property(client => client.Cnh)
            .HasMaxLength(20);

        builder.Property(client => client.Cnpj)
            .HasMaxLength(18);

        builder.OwnsOne(client => client.Address, address =>
        {
            address.Property(a => a.State)
                .IsRequired()
                .HasMaxLength(100);

            address.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(150);

            address.Property(a => a.District)
                .IsRequired()
                .HasMaxLength(150);

            address.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200);

            address.Property(a => a.Number)
                .IsRequired()
                .HasMaxLength(20);
        });
    }
}