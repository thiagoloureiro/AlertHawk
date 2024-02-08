using System.Globalization;
using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Helpers;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

public class UserRepository : BaseRepository, IUserRepository
{
    private readonly IMapper _mapper;

    public UserRepository(IConfiguration configuration, IMapper mapper) : base(configuration)
    {
        _mapper = mapper;
    }

    public async Task<UserDto?> Get(Guid id)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon FROM Users WHERE Id = @Id";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByEmail(string email)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon FROM Users WHERE LOWER(Email) = LOWER(@Email)";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql, new { Email = email.ToLower(CultureInfo.InvariantCulture) });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByUsername(string username)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon FROM Users WHERE LOWER(Username) = LOWER(@Username)";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql, new { Username = username.ToLower(CultureInfo.InvariantCulture) });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>?> GetAll()
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, CreatedAt, UpdatedAt, LastLogon FROM Users";
        var user = await ExecuteQueryAsync<User>(sql);
        return _mapper.Map<List<UserDto>?>(user.ToList());
    }

    public async Task Create(UserCreation userCreation)
    {
        const string checkExistingUserSql = "SELECT Id FROM Users WHERE LOWER(Email) = @Email OR LOWER(Username) = @Username";
        var existingUser = await ExecuteQueryFirstOrDefaultAsync<Guid?>(checkExistingUserSql, new
        {
            Username = userCreation.Username.ToLower(CultureInfo.InvariantCulture),
            Email = userCreation.UserEmail.ToLower(CultureInfo.InvariantCulture)
        });

        if (existingUser.HasValue)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var salt = PasswordHasher.GenerateSalt();
        var hashedPassword = PasswordHasher.HashPassword(userCreation.Password, salt);

        const string insertUserSql = @"
            INSERT INTO Users (Id, Username, Email, Password, Salt, IsAdmin, CreatedAt) 
            VALUES (NEWID(), @Username, @Email, @Password, @Salt, @IsAdmin, @CreatedAt)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            Username = userCreation.Username,
            Email = userCreation.UserEmail.ToLower(CultureInfo.InvariantCulture),
            Password = hashedPassword,
            Salt = salt,
            IsAdmin = userCreation.IsAdmin,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task Update(Guid id, UserUpdate userUpdate)
    {
        bool updateUsername = !string.IsNullOrWhiteSpace(userUpdate.Username);
        bool updateEmail = !string.IsNullOrWhiteSpace(userUpdate.UserEmail);

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        // Check if the new username or email is already taken by another user
        if (updateUsername || updateEmail)
        {
            var conditions = new List<string>();

            if (updateUsername)
            {
                conditions.Add("LOWER(Username) = LOWER(@Username)");
                parameters.Add("Username", userUpdate.Username);
            }

            if (updateEmail)
            {
                conditions.Add("LOWER(Email) = LOWER(@Email)");
                parameters.Add("Email", userUpdate.UserEmail?.ToLower(CultureInfo.InvariantCulture));
            }

            var checkUserSql = $@"
                SELECT Id 
                FROM Users 
                WHERE ({string.Join(" OR ", conditions)})
                AND Id != @Id";

            var existingUser = await ExecuteQueryFirstOrDefaultAsync<Guid?>(checkUserSql, parameters);
            if (existingUser.HasValue)
            {
                throw new InvalidOperationException("Username or Email already exists.");
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
            parameters.Add("Email", userUpdate.UserEmail?.ToLower(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(userUpdate.NewPassword))
        {
            var salt = PasswordHasher.GenerateSalt();
            var hashedPassword = PasswordHasher.HashPassword(userUpdate.NewPassword, salt);

            updateFields.Add("Password = @Password");
            updateFields.Add("Salt = @Salt");
            parameters.Add("Password", hashedPassword);
            parameters.Add("Salt", salt);
        }
        updateFields.Add("UpdatedAt = @UpdatedAt");
        parameters.Add("UpdatedAt", DateTime.UtcNow);

        if (updateFields.Any())
        {
            var updateSql = $"UPDATE Users SET {string.Join(", ", updateFields)} WHERE Id = @Id";
            await ExecuteNonQueryAsync(updateSql, parameters);
        }
    }

    public async Task<string> ResetPassword(string username)
    {
        var newPassword = PasswordHasher.GenerateRandomPassword(10);
        var salt = PasswordHasher.GenerateSalt();
        var hashedPassword = PasswordHasher.HashPassword(newPassword, salt);

        const string insertUserSql = @"UPDATE Users SET Password = @Password, Salt = @Salt, UpdatedAt = @UpdatedAt WHERE LOWER(Username) = LOWER(@Username)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            Username = username.ToLower(CultureInfo.InvariantCulture),
            Password = hashedPassword,
            Salt = salt,
            UpdatedAt = DateTime.UtcNow
        });
        return hashedPassword;
    }

    public async Task<UserDto?> Login(string username, string password)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, Password, Salt, CreatedAt, UpdatedAt, LastLogon FROM Users WHERE LOWER(Username) = LOWER(@username)";
        var user = await ExecuteQueryFirstOrDefaultAsync<User>(sql, new { username = username.ToLower(CultureInfo.InvariantCulture) });

        if (user is null || !PasswordHasher.VerifyPassword(password, user.Password, user.Salt))
        {
            return null;
        }

        await ExecuteNonQueryAsync("UPDATE Users SET LastLogon = @LastLogon WHERE Id = @Id", new { LastLogon = DateTime.UtcNow, Id = user.Id });

        return _mapper.Map<UserDto>(user);
    }


}
