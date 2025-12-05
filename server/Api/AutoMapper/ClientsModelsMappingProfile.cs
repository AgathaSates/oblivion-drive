using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.ClientModule.Requests;
using OblivionDrive.Api.Models.ClientModule.Responses;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Application.ClientModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public sealed class ClientsModelsMappingProfile : Profile
{
    public ClientsModelsMappingProfile()
    {
        CreateMap<RegisterClientRequest, CreateClientCommand>();
        CreateMap<ClientDTO, RegisterClientResponse>();

        CreateMap<(Guid, UpdateClientRequest), UpdateClientCommand>()
            .ConvertUsing(src => new UpdateClientCommand(
                src.Item1,
                src.Item2.Name,
                src.Item2.Email,
                src.Item2.PhoneNumber,
                src.Item2.ClientType,
                src.Item2.Cpf,
                src.Item2.Rg,
                src.Item2.Cnh,
                src.Item2.Cnpj,
                src.Item2.State,
                src.Item2.City,
                src.Item2.District,
                src.Item2.Street,
                src.Item2.Number
            ));

        CreateMap<UpdatedClientDTO, UpdateClientResponse>();
        CreateMap<DetailClientDTO, GetClientByIdResponse>();
        CreateMap<ClientsResult, GetAllClientsResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllClientsResponse(
                src.Clients.Count,
                src?.Clients?
                    .Select(client => ctx.Mapper.Map<DetailClientDTO>(client))
                    .ToImmutableList() ?? ImmutableList<DetailClientDTO>.Empty
            ));
    }
}