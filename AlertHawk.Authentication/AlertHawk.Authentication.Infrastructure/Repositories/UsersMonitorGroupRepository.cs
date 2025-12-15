using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using AlertHawk.Authentication.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
public class UsersMonitorGroupRepository : BaseRepository, IUsersMonitorGroupRepository
{
    public UsersMonitorGroupRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task CreateAsync(UsersMonitorGroup usersMonitorGroup)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UsersMonitorGroup", DatabaseProvider);
        var sql = $"INSERT INTO {tableName} (Id, UserId, GroupMonitorId) VALUES (@Id, @UserId, @GroupMonitorId)";
        await ExecuteNonQueryAsync(sql, new { usersMonitorGroup.Id, usersMonitorGroup.UserId, usersMonitorGroup.GroupMonitorId });
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UsersMonitorGroup", DatabaseProvider);
        var sql = $"DELETE FROM {tableName} WHERE UserId = @userId";
        await ExecuteNonQueryAsync(sql, new { userId });
    }

    public async Task<IEnumerable<UsersMonitorGroup>> GetAsync(Guid userId)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UsersMonitorGroup", DatabaseProvider);
        var sql = $"SELECT Id, UserId, GroupMonitorId FROM {tableName} WHERE UserId = @userId";
        return await ExecuteQueryAsyncWithParameters<UsersMonitorGroup>(sql, new { userId }) ?? Array.Empty<UsersMonitorGroup>();
    }

    public async Task DeleteAllByGroupMonitorIdAsync(int groupMonitorId)
    {
        var tableName = Helpers.DatabaseProvider.FormatTableName("UsersMonitorGroup", DatabaseProvider);
        var sql = $"DELETE FROM {tableName} WHERE GroupMonitorId = @groupMonitorId";
        await ExecuteNonQueryAsync(sql, new { groupMonitorId });
    }
}