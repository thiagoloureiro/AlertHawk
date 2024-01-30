using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUserRepository
{
    Task Create(UserCreation userCreation);

    Task<UserDto?> Login(string email, string password);

    Task<UserDto?> Get(Guid id);

    Task<UserDto?> GetByEmail(string email);
}