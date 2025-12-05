using AutoMapper;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Application.AutoMapper;
public class ClientMappingProfile : Profile
{
    public ClientMappingProfile()
    {
        CreateMap<Client, ClientDTO>()
            .ConstructUsing(client => new ClientDTO(
                true,
                client.Name,
                client.Email,
                client.PhoneNumber,
                client.ClientType,
                client.Cpf,
                client.Rg,
                client.Cnh,
                client.Cnpj,
                client.Address.State,
                client.Address.City,
                client.Address.District,
                client.Address.Street,
                client.Address.Number));

        CreateMap<Client, UpdatedClientDTO>()
            .ConstructUsing(client => new UpdatedClientDTO(
                true,
                client.Name,
                client.Email,
                client.PhoneNumber,
                client.ClientType,
                client.Cpf,
                client.Rg,
                client.Cnh,
                client.Cnpj,
                client.Address.State,
                client.Address.City,
                client.Address.District,
                client.Address.Street,
                client.Address.Number));

        CreateMap<Client, DetailClientDTO>()
            .ConstructUsing(client => new DetailClientDTO(
                client.Id,
                client.Name,
                client.Email,
                client.PhoneNumber,
                client.ClientType,
                client.Cpf,
                client.Rg,
                client.Cnh,
                client.Cnpj,
                client.Address.State,
                client.Address.City,
                client.Address.District,
                client.Address.Street,
                client.Address.Number));
    }
}
