using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.AutoMapper;

[ExcludeFromCodeCoverage]
public static class UserMapper
{
    public static UserDto? MapToDto(User? user)
    {
        return user == null ? null : new UserDto(user.Id, user.Username, user.Email, user.IsAdmin);
    }

    public static IEnumerable<UserDto>? MapToDtoList(IEnumerable<User>? users)
    {
        return users?.Select(u => new UserDto(u.Id, u.Username, u.Email, u.IsAdmin)).ToList();
    }
}