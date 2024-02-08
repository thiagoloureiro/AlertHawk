using AlertHawk.Authentication.Application.Interfaces;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.EmailSender;
using AlertHawk.Authentication.Infrastructure.Interfaces;

namespace AlertHawk.Authentication.Application.Services;

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
    
    public async Task Update(Guid id, UserUpdate userUpdate)
    {
        await _userRepository.Update(id, userUpdate);
    }

    public async Task ResetPassword(string username)
    {
        var user = await GetByUsername(username);
        if (user != null)
        {
            var password = await _userRepository.ResetPassword(username);
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

    public Task<IEnumerable<UserDto>?> GetAll()
    {
        return _userRepository.GetAll();
    }

    public async Task<UserDto?> GetByEmail(string email)
    {
        return await _userRepository.GetByEmail(email);
    }

    public async Task<UserDto?> GetByUsername(string username)
    {
        return await _userRepository.GetByUsername(username);
    }
}