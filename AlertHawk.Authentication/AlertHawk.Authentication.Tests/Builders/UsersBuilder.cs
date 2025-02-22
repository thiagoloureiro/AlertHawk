using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Tests.Builders;

public class UsersBuilder
{
    private UserDto? _userDto;
    private UserCreation? _userCreation;
    private UserAuth? _userAuth;

    public UserDto WithUserEmailAndAdminIsFalse(string email)
    {
        _userDto = new UserDto(Guid.NewGuid(), "testNotAdminuser", email, false);
        return _userDto;
    }

    public UserDto WithUserEmailAndAdminIsTrue(string email)
    {
        _userDto = new UserDto(Guid.NewGuid(), "testAdminuser", email, true);
        return _userDto;
    }

    public UserCreation WithUserCreationWithTheSamePasswordData()
    {
        _userCreation = new UserCreation
        {
            Password = "password",
            RepeatPassword = "password",
            Username = "",
            UserEmail = ""
        };
        return _userCreation;
    }

    public UserAuth WithUserAuth()
    {
        _userAuth = new UserAuth { username = "testuser@user.com", Password = "wrongpassword" };
        return _userAuth;
    }
}