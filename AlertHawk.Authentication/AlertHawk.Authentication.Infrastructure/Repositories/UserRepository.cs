using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Helpers;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UserRepository : BaseRepository, IUserRepository
{
    private readonly IMapper _mapper;
    private readonly IUsersMonitorGroupRepository _usersMonitorGroupRepository;

    public UserRepository(IConfiguration configuration, IMapper mapper, IUsersMonitorGroupRepository usersMonitorGroupRepository) : base(configuration)
    {
        _mapper = mapper;
        _usersMonitorGroupRepository = usersMonitorGroupRepository;
    }

    public async Task<UserDto?> Get(Guid id)
    {
        const string sql =
            "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon  FROM Users WHERE Id = @Id";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByEmail(string email)
    {
        const string sql =
            "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon  FROM Users WHERE LOWER(Email) = LOWER(@Email)";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql,
            new { Email = email.ToLower(CultureInfo.InvariantCulture) });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByUsername(string username)
    {
        const string sql =
            "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon  FROM Users WHERE LOWER(Username) = LOWER(@Username)";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql,
            new { Username = username.ToLower(CultureInfo.InvariantCulture) });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>?> GetAll()
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon FROM Users";
        var user = await ExecuteQueryAsync<User>(sql);
        return _mapper.Map<List<UserDto>?>(user);
    }

    public async Task Delete(Guid id)
    {
        await _usersMonitorGroupRepository.DeleteAllByUserIdAsync(id);
        
        const string sql = "DELETE FROM Users WHERE Id = @Id";
        await ExecuteNonQueryAsync(sql, new { Id = id });
    }

    public async Task<UserDto?> GetUserByToken(string? jwtToken)
    {
        const string sql = "SELECT Username, Email, IsAdmin FROM Users WHERE Token = @jwtToken";
        var user =  await ExecuteQueryFirstOrDefaultAsync<User>(sql, new { jwtToken });
        return _mapper.Map<UserDto>(user);
    }

    public async Task UpdateUserToken(string token, string username)
    {
        const string sql = "UPDATE Users SET Token = @token WHERE LOWER(Username) = @username";
        await ExecuteNonQueryAsync(sql, new { token, username });
    }

    public async Task Create(UserCreation userCreation)
    {
        string checkExistingUserSql = "SELECT Id FROM Users WHERE LOWER(Email) = @Email";
        var existingUser = await ExecuteQueryFirstOrDefaultAsync<Guid?>(checkExistingUserSql, new
        {
            Email = userCreation.UserEmail.ToLower(CultureInfo.InvariantCulture)
        });

        if (existingUser.HasValue)
        {
            throw new InvalidOperationException("The email is already registered, please choose another.");
        }

        checkExistingUserSql = "SELECT Id FROM Users WHERE LOWER(Username) = @Username";
        existingUser = await ExecuteQueryFirstOrDefaultAsync<Guid?>(checkExistingUserSql, new
        {
            Username = userCreation.Username.ToLower(CultureInfo.InvariantCulture)
        });
        if (existingUser.HasValue)
        {
            throw new InvalidOperationException("The username is already registered, please choose another.");
        }

        var salt = PasswordHasher.GenerateSalt();
        var hashedPassword = PasswordHasher.HashPassword(userCreation.Password, salt);

        const string insertUserSql = @"
            INSERT INTO Users (Id, Username, Email, Password, Salt, IsAdmin, CreatedAt) 
            VALUES (NEWID(), @Username, @Email, @Password, @Salt, @IsAdmin, @CreatedAt)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            userCreation.Username,
            Email = userCreation.UserEmail.ToLower(CultureInfo.InvariantCulture),
            Password = hashedPassword,
            Salt = salt,
            userCreation.IsAdmin,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task CreateFromAzure(UserCreationFromAzure userCreation)
    {
        const string insertUserSql = @"
            INSERT INTO Users (Id, Username, Email, IsAdmin, CreatedAt) 
            VALUES (NEWID(), @Username, @Email, @IsAdmin, @CreatedAt)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            userCreation.Username,
            Email = userCreation.Email.ToLower(CultureInfo.InvariantCulture),
            CreatedAt = DateTime.UtcNow,
            userCreation.IsAdmin
        });
    }

    public async Task Update(UserDto userUpdate)
    {
        bool updateUsername = !string.IsNullOrWhiteSpace(userUpdate.Username);
        bool updateEmail = !string.IsNullOrWhiteSpace(userUpdate.Email);

        var parameters = new DynamicParameters();
        parameters.Add("Id", userUpdate.Id);

        // Check if the new username or email is already taken by another user
        if (updateUsername || updateEmail)
        {
            var conditions = new List<string>();

            if (updateUsername)
            {
                conditions.Add("LOWER(Username) = LOWER(@Username)");
                parameters.Add("Username", userUpdate.Username);
            }

            var checkUserSql = $@"
                SELECT Id 
                FROM Users 
                WHERE ({string.Join(" OR ", conditions)})
                AND Id != @Id";

            var existingUser = await ExecuteQueryFirstOrDefaultAsync<Guid?>(checkUserSql, parameters);
            if (existingUser.HasValue)
            {
                throw new InvalidOperationException("Email already exists.");
            }

            if (updateEmail)
            {
                conditions.Add("LOWER(Email) = LOWER(@Email)");
                parameters.Add("Email", userUpdate.Email.ToLower(CultureInfo.InvariantCulture));
            }

            existingUser = await ExecuteQueryFirstOrDefaultAsync<Guid?>(checkUserSql, parameters);
            if (existingUser.HasValue)
            {
                throw new InvalidOperationException("Email already exists.");
            }
        }

        var updateFields = new List<string>();
        if (updateUsername)
        {
            updateFields.Add("Username = @Username");
            parameters.Add("Username", userUpdate.Username);
        }

        if (updateEmail)
        {
            updateFields.Add("Email = @Email");
            parameters.Add("Email", userUpdate.Email?.ToLower(CultureInfo.InvariantCulture));
        }

        if (updateFields.Count != 0)
        {
            updateFields.Add("UpdatedAt = @UpdatedAt");
            updateFields.Add("IsAdmin = @IsAdmin");
            parameters.Add("UpdatedAt", DateTime.UtcNow);
            parameters.Add("IsAdmin", userUpdate.IsAdmin);

            var updateSql = $"UPDATE Users SET {string.Join(", ", updateFields)} WHERE Id = @Id";
            await ExecuteNonQueryAsync(updateSql, parameters);
        }
    }

    public async Task<string> ResetPassword(string email)
    {
        var newPassword = PasswordHasher.GenerateRandomPassword(10);
        var salt = PasswordHasher.GenerateSalt();
        var hashedPassword = PasswordHasher.HashPassword(newPassword, salt);

        const string insertUserSql =
            @"UPDATE User SET Password = @Password, Salt = @Salt, UpdatedAt = @UpdatedAt WHERE LOWER(email) = LOWER(@email)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            Email = email.ToLower(CultureInfo.InvariantCulture),
            Password = hashedPassword,
            Salt = salt,
            UpdatedAt = DateTime.UtcNow
        });
        return hashedPassword;
    }

    public async Task<UserDto?> Login(string username, string password)
    {
        const string sql =
            "SELECT Id, Email, Username, IsAdmin, Password, Salt, CreatedAt, UpdatedAt, LastLogon  FROM Users WHERE LOWER(Username) = LOWER(@username)";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql,
            new { username = username.ToLower(CultureInfo.InvariantCulture) });

        if (user is null || !PasswordHasher.VerifyPassword(password, user.Password, user.Salt))
        {
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }
}