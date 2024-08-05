using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AutoMapper;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.AutoMapper;

[ExcludeFromCodeCoverage]
public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<User, UserDto>();
    }
}