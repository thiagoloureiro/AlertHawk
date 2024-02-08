using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Application.Interfaces;

public interface IUserService
{
    Task Create(UserCreation userCreation);
    
    Task Update(Guid id, UserUpdate userUpdate);
    
    Task ResetPassword(string username);

    Task<UserDto?> Login(string username, string password);

    Task<UserDto?> Get(Guid id);
    Task<IEnumerable<UserDto>?> GetAll();

    Task<UserDto?> GetByEmail(string email);
    
    Task<UserDto?> GetByUsername(string username);
}