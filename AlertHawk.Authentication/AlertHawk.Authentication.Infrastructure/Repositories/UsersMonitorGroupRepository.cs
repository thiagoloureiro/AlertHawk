using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

public class UsersMonitorGroupRepository : BaseRepository, IUsersMonitorGroupRepository
{
    public UsersMonitorGroupRepository(IConfiguration configuration) : base(configuration)
    {
    }
    public async Task CreateAsync(UsersMonitorGroup usersMonitorGroup)
    {
        const string sql = "INSERT INTO [dbo].[UsersMonitorGroup] ([Id], [UserId], [GroupMonitorId]) VALUES (@Id, @UserId, @GroupMonitorId)";
        await ExecuteNonQueryAsync(sql, new { usersMonitorGroup.Id, usersMonitorGroup.UserId, usersMonitorGroup.GroupMonitorId });
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        const string sql = "delete [dbo].[UsersMonitorGroup] where UserId = @userId";
        await ExecuteNonQueryAsync(sql, new { userId });
    }

    public async Task<IEnumerable<UsersMonitorGroup>> GetAsync(Guid userId)
    {
        const string sql = "SELECT [Id], [UserId], [GroupMonitorId] FROM [dbo].[UsersMonitorGroup] where UserId = @userId";
        return await ExecuteQueryAsyncWithParameters<UsersMonitorGroup>(sql, new { userId }) ?? Array.Empty<UsersMonitorGroup>();
    }

    public async Task DeleteAllByGroupMonitorIdAsync(int groupMonitorId)
    {
        const string sql = "delete [dbo].[UsersMonitorGroup] where GroupMonitorId = @groupMonitorId";
        await ExecuteNonQueryAsync(sql, new { groupMonitorId });
    }
}