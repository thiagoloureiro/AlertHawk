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

    public async Task Create(UserCreation userCreation)
    {
        const string checkExistingUserSql = "SELECT Id FROM Users WHERE Email = @Email OR Username = @Username";
        var existingUser = await ExecuteQueryAsync<Guid?>(checkExistingUserSql, new
        {
            Username = userCreation.Username,
            Email = userCreation.UserEmail
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
            Email = userCreation.UserEmail,
            Password = hashedPassword,
            Salt = salt,
            IsAdmin = userCreation.IsAdmin
        });
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
