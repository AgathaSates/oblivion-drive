using AutoMapper;
using OblivionDrive.Api.Models.AuthenticationModule;
using OblivionDrive.Application.AuthenticationModule.Commands;

namespace OblivionDrive.Api.AutoMapper;

public class AuthenticationModelsMappingProfile : Profile
{
    public AuthenticationModelsMappingProfile()
    {
        CreateMap<RegisterUserRequest, RegisterUserCommand>();
        CreateMap<LoginUserRequest, LoginUserCommand>();
    }
}