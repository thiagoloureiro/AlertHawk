using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Interfaces;

public interface IUserService
{
    Task Create(UserCreation userCreation);
    
    Task ResetPassword(string username);

    Task<UserDto?> Login(string username, string password);

    Task<UserDto?> Get(Guid id);

    Task<UserDto?> GetByEmail(string email);
    
    Task<UserDto?> GetByUsername(string username);
}