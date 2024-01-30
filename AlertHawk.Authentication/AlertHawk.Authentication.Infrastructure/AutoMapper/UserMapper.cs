using System.Diagnostics.CodeAnalysis;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AutoMapper;

namespace AlertHawk.Authentication.Infrastructure.AutoMapper;

[ExcludeFromCodeCoverage]
public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<User, UserDto>();
    }
}