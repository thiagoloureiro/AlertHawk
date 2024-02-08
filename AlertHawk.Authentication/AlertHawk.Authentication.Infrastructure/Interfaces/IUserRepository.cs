using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUserRepository
{
    Task Create(UserCreation userCreation);
    
    Task Update(Guid id, UserUpdate userUpdate);
    
    Task<string> ResetPassword(string username);

    Task<UserDto?> Login(string username, string password);

    Task<UserDto?> Get(Guid id);

    Task<UserDto?> GetByEmail(string email);
    Task<UserDto?> GetByUsername(string username);
    Task<IEnumerable<UserDto>?> GetAll();
}