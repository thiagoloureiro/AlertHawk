using AlertHawk.Authentication.Domain.Dto;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Helpers;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

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
        const string sql = "SELECT Id, Email, Username, IsAdmin FROM Users WHERE Id = @Id"; 
        var user = await ExecuteQueryAsync<User>(sql, new { Id = id });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByEmail(string email)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin FROM Users WHERE LOWER(Email) = LOWER(@Email)";
        var user = await ExecuteQueryAsync<User>(sql, new { Email = email.ToLower() });
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetByUsername(string username)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin FROM Users WHERE LOWER(Username) = LOWER(@Username)";
        var user = await ExecuteQueryAsync<User>(sql, new { Username = username.ToLower() });
        return _mapper.Map<UserDto>(user);
    }

    public async Task Create(UserCreation userCreation)
    {
        const string checkExistingUserSql = "SELECT Id FROM Users WHERE LOWER(Email) = @Email OR LOWER(Username) = @Username";
        var existingUser = await ExecuteQueryAsync<Guid?>(checkExistingUserSql, new
        {
            Username = userCreation.Username.ToLower(),
            Email = userCreation.UserEmail.ToLower()
        });

        if (existingUser.HasValue)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var salt = PasswordHasher.GenerateSalt();
        var hashedPassword = PasswordHasher.HashPassword(userCreation.Password, salt);

        const string insertUserSql = @"
            INSERT INTO Users (Id, Username, Email, Password, Salt, IsAdmin) 
            VALUES (NEWID(), @Username, @Email, @Password, @Salt, @IsAdmin)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            Username = userCreation.Username,
            Email = userCreation.UserEmail.ToLower(),
            Password = hashedPassword,
            Salt = salt,
            IsAdmin = userCreation.IsAdmin
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
                parameters.Add("Email", userUpdate.UserEmail.ToLower());
            }

            var checkUserSql = $@"
                SELECT Id 
                FROM Users 
                WHERE ({string.Join(" OR ", conditions)})
                AND Id != @Id";

            var existingUser = await ExecuteQueryAsync<Guid?>(checkUserSql, parameters);
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
            parameters.Add("Email", userUpdate.UserEmail?.ToLower());
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
        
        const string insertUserSql = @"UPDATE User SET Password = @Password, Salt = @Salt WHERE LOWER(Username) = LOWER(@Username)";

        await ExecuteNonQueryAsync(insertUserSql, new
        {
            Username = username.ToLower(),
            Password = hashedPassword,
            Salt = salt
        });
        return hashedPassword;
    }

    public async Task<UserDto?> Login(string username, string password)
    {
        const string sql = "SELECT Id, Email, Username, IsAdmin, Password, Salt FROM Users WHERE LOWER(Username) = LOWER(@username)";
        var user = await ExecuteQueryAsync<User>(sql, new { username = username.ToLower() });

        if (user is null || !PasswordHasher.VerifyPassword(password, user.Password, user.Salt))
        {
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }
    
    private async Task<T?> ExecuteQueryAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }
    
    private async Task ExecuteNonQueryAsync(string sql, object? parameters = null)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, parameters);
    }
}
