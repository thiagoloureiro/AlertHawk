using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
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
}