using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUserRepository
{
    Task Create(UserCreation userCreation);

    Task CreateFromAzure(UserCreationFromAzure userCreation);

    Task Update(UserDto userUpdate);

    Task<string?> ResetPassword(string email);

    Task<UserDto?> Login(string username, string password);

    Task<UserDto?> Get(Guid id);

    Task<UserDto?> GetByEmail(string email);

    Task<UserDto?> GetByUsername(string username);

    Task<IEnumerable<UserDto>?> GetAll();

    Task Delete(Guid id);

    Task<UserDto?> GetUserByToken(string? jwtToken);

    Task UpdateUserToken(string token, string username);

    Task UpdatePassword(string email, string password);

    Task<UserDto?> LoginWithEmail(string email, string userPasswordCurrentPassword);
    Task UpdateUserDeviceToken(string deviceToken, Guid userId);
    Task<IEnumerable<string>?> GetUserDeviceTokenList(Guid userId);
    Task<IEnumerable<UserDto>?> GetAllByGroupId(int groupId);
}