using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.EmailSender;
using AlertHawk.Authentication.Infrastructure.Interfaces;

namespace AlertHawk.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Create(UserCreation userCreation)
    {
        await _userRepository.Create(userCreation);
    }

    public async Task CreateFromAzure(UserCreationFromAzure userCreation)
    {
        var userList = await _userRepository.GetAll();
        if (userList != null && !userList.Any())
        {
            userCreation.IsAdmin = true;
        }

        await _userRepository.CreateFromAzure(userCreation);
    }

    public async Task Update(UserDto userUpdate)
    {
        await _userRepository.Update(userUpdate);
    }

    public async Task Delete(Guid id)
    {
        await _userRepository.Delete(id);
    }

    public async Task ResetPassword(string email)
    {
        var user = await GetByEmail(email);
        if (user != null)
        {
            var password = await _userRepository.ResetPassword(email);
            EmailSender.SendEmail(user.Email, "Password Reset",
                $"This is a password reset, your new password is {password}");
        }
    }

    public async Task<UserDto?> Login(string username, string password)
    {
        return await _userRepository.Login(username, password);
    }

    public async Task<UserDto?> Get(Guid id)
    {
        return await _userRepository.Get(id);
    }

    public async Task<UserDto?> GetByEmail(string email)
    {
        return await _userRepository.GetByEmail(email);
    }

    public async Task<UserDto?> GetByUsername(string username)
    {
        return await _userRepository.GetByUsername(username);
    }

    public Task<IEnumerable<UserDto>?> GetAll()
    {
        return _userRepository.GetAll();
    }

    public async Task<UserDto?> GetUserByToken(string? jwtToken)
    {
        return await _userRepository.GetUserByToken(jwtToken);
    }

    public async Task UpdateUserToken(string token, string username)
    {
        await _userRepository.UpdateUserToken(token, username);
    }

    public async Task UpdatePassword(string email, string password)
    {
        await _userRepository.UpdatePassword(email, password);
    }

    public async Task<bool> LoginWithEmail(string email, string userPasswordCurrentPassword)
    {
        var user = await GetByEmail(email);
        if (user != null)
        {
            return await _userRepository.LoginWithEmail(email, userPasswordCurrentPassword);
        }

        return false;
    }
}