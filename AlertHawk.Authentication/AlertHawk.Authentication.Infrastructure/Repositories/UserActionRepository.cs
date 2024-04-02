using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Authentication.Infrastructure.Repositories;

public class UserActionRepository : BaseRepository, IUserActionRepository
{
    public UserActionRepository(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task CreateAsync(UserAction userAction)
    {
        const string sql = "INSERT INTO [dbo].[UserAction] ([UserId], [Action], [ActionTime]) VALUES (@UserId, @Action, @TimeStamp)";
        await ExecuteNonQueryAsync(sql, new { userAction.UserId, userAction.Action, userAction.TimeStamp });
    }

    public async Task<IEnumerable<UserAction>> GetAsync()
    {
        const string sql = "SELECT [UserId], [Action], [TimeStamp] FROM [dbo].[UserAction]";
        return await ExecuteQueryAsync<UserAction>(sql) ?? Array.Empty<UserAction>();
    }
}