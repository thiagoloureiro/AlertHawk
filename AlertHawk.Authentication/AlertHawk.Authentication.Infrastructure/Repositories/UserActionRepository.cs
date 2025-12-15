using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UserActionRepository : BaseRepository, IUserActionRepository
{
    public UserActionRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task CreateAsync(UserAction userAction)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UserAction", DatabaseProvider);
        var sql = $"INSERT INTO {tableName} (UserId, Action, TimeStamp) VALUES (@UserId, @Action, @TimeStamp)";
        await ExecuteNonQueryAsync(sql, new { userAction.UserId, userAction.Action, userAction.TimeStamp });
    }

    public async Task<IEnumerable<UserAction>> GetAsync()
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UserAction", DatabaseProvider);
        var sql = $"SELECT UserId, Action, TimeStamp FROM {tableName}";
        return await ExecuteQueryAsync<UserAction>(sql) ?? Array.Empty<UserAction>();
    }
}