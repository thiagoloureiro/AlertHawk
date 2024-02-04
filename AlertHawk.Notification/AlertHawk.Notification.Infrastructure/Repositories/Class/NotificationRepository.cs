using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Notification.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class NotificationRepository : RepositoryBase, INotificationRepository
{
    private readonly string _connstring;

    public NotificationRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<NotificationItem>> SelectNotificationItemList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT Id, Name, Description, NotificationTypeId FROM [NotificationItem]";

        var notificationItemList = await db.QueryAsync<NotificationItem>(sql, commandType: CommandType.Text);

        var notificationEmailList = await SelectNotificationEmailList();
        var notificationTeamsList = await SelectNotificationTeamsList();
        var notificationSlackList = await SelectNotificationSlackList();
        var notificationTelegramList = await SelectNotificationTelegramList();

        var selectNotificationItemList = notificationItemList.ToList();
        foreach (var item in selectNotificationItemList)
        {
            switch (item.NotificationTypeId)
            {
                case 1: // Smtp
                    item.NotificationEmail = notificationEmailList.SingleOrDefault(x => x.NotificationId == item.Id);
                    break;
                case 2: // Teams
                    item.NotificationTeams = notificationTeamsList.SingleOrDefault(x => x.NotificationId == item.Id);
                    break;
                case 3: // Telegram
                    item.NotificationTelegram =
                        notificationTelegramList.SingleOrDefault(x => x.NotificationId == item.Id);
                    break;
                case 4: // Slack
                    item.NotificationSlack = notificationSlackList.SingleOrDefault(x => x.NotificationId == item.Id);
                    break;
            }
        }

        return selectNotificationItemList;
    }


    public async Task InsertNotificationItemEmailSmtp(NotificationItem notificationItem)
    {
        var notificationId = await InsertNotificationItem(notificationItem);

        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationEmailSmtp] (NotificationId, FromEmail, ToEmail, HostName, Port, Username, Password, ToCCEmail, ToBCCEmail, EnableSSL, Subject, Body, IsHtmlBody) 
        VALUES (@notificationId, @FromEmail, @ToEmail, @HostName, @Port, @Username, @Password, @ToCCEmail, @ToBCCEmail, @EnableSSL, @Subject, @Body, @IsHtmlBody)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId,
            notificationItem.NotificationEmail?.FromEmail, notificationItem.NotificationEmail?.ToEmail,
            notificationItem.NotificationEmail?.Hostname, notificationItem.NotificationEmail?.Port,
            notificationItem.NotificationEmail?.Username, notificationItem.NotificationEmail?.Password,
            notificationItem.NotificationEmail?.ToCCEmail, notificationItem.NotificationEmail?.ToBCCEmail,
            notificationItem.NotificationEmail?.EnableSsl, notificationItem.NotificationEmail?.Subject,
            notificationItem.NotificationEmail?.Body, notificationItem.NotificationEmail?.IsHtmlBody
        }, commandType: CommandType.Text);
    }

    public async Task UpdateNotificationItem(NotificationItem notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"UPDATE [NotificationItem] SET Name = @Name, NotificationTypeId = @NotificationTypeId, Description = @Description WHERE Id = @Id";

        await db.ExecuteAsync(sql,
            new
            {
                Name = notificationItem.Name,
                NotificationTypeId = notificationItem.NotificationTypeId,
                Id = notificationItem.Id,
                Description = notificationItem.Description
            },
            commandType: CommandType.Text);

        await DeleteNotificationItemFromChilds(notificationItem.Id);
    }

    public async Task InsertNotificationItemMsTeams(NotificationItem notificationItem)
    {
        var notificationId = await InsertNotificationItem(notificationItem);

        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationTeams] (NotificationId, WebHookUrl) VALUES (@notificationId, @WebHookUrl)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId,
            notificationItem.NotificationTeams?.WebHookUrl
        }, commandType: CommandType.Text);
    }

    public async Task InsertNotificationItemTelegram(NotificationItem notificationItem)
    {
        var notificationId = await InsertNotificationItem(notificationItem);

        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationTelegram] (NotificationId, ChatId, TelegramBotToken) VALUES (@notificationId, @ChatId, @TelegramBotToken)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId,
            notificationItem.NotificationTelegram?.ChatId,
            notificationItem.NotificationTelegram?.TelegramBotToken,
        }, commandType: CommandType.Text);
    }

    public async Task InsertNotificationItemSlack(NotificationItem notificationItem)
    {
        var notificationId = await InsertNotificationItem(notificationItem);

        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationSlack] (NotificationId, WebHookUrl, Channel) VALUES (@notificationId, @WebHookUrl, @ChannelName)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId,
            notificationItem.NotificationSlack?.WebHookUrl,
            ChannelName = notificationItem.NotificationSlack?.Channel
        }, commandType: CommandType.Text);
    }

    public async Task<NotificationItem?> SelectNotificationItemById(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT Id, Name, Description, NotificationTypeId FROM [NotificationItem]";

        var notificationItemList = await db.QueryAsync<NotificationItem>(sql, commandType: CommandType.Text);
        var notificationItem = notificationItemList.FirstOrDefault(x => x.Id == id);

        var notificationEmailList = await SelectNotificationEmailList();
        var notificationTeamsList = await SelectNotificationTeamsList();
        var notificationSlackList = await SelectNotificationSlackList();
        var notificationTelegramList = await SelectNotificationTelegramList();

        switch (notificationItem?.NotificationTypeId)
        {
            case 1: // Smtp
                notificationItem.NotificationEmail =
                    notificationEmailList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                break;
            case 2: // Teams
                notificationItem.NotificationTeams =
                    notificationTeamsList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                break;
            case 3: // Telegram
                notificationItem.NotificationTelegram =
                    notificationTelegramList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                break;
            case 4: // Slack
                notificationItem.NotificationSlack =
                    notificationSlackList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                break;
        }

        return notificationItem;
    }

    public async Task<IEnumerable<NotificationItem>> SelectNotificationItemList(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT Id, Name, Description, NotificationTypeId FROM [NotificationItem] WHERE id IN @ids";

        var notificationItemList =
            await db.QueryAsync<NotificationItem>(sql, new { ids }, commandType: CommandType.Text);

        var notificationEmailList = await SelectNotificationEmailList();
        var notificationTeamsList = await SelectNotificationTeamsList();
        var notificationSlackList = await SelectNotificationSlackList();
        var notificationTelegramList = await SelectNotificationTelegramList();

        var selectNotificationItemList = notificationItemList.ToList();
        foreach (var notificationItem in selectNotificationItemList)
        {
            switch (notificationItem?.NotificationTypeId)
            {
                case 1: // Smtp
                    notificationItem.NotificationEmail =
                        notificationEmailList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                    break;
                case 2: // Teams
                    notificationItem.NotificationTeams =
                        notificationTeamsList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                    break;
                case 3: // Telegram
                    notificationItem.NotificationTelegram =
                        notificationTelegramList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                    break;
                case 4: // Slack
                    notificationItem.NotificationSlack =
                        notificationSlackList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                    break;
            }
        }

        return selectNotificationItemList;
    }

    public async Task DeleteNotificationItem(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"DELETE FROM [NotificationItem] WHERE Id = @id";

        await db.ExecuteAsync(sql, new
        {
            Id = id
        }, commandType: CommandType.Text);

        await DeleteNotificationItemFromChilds(id);
    }

    private async Task<int> InsertNotificationItem(NotificationItem notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [NotificationItem] (Name, NotificationTypeId, Description) VALUES (@Name, @NotificationTypeId, @Description); SELECT CAST(SCOPE_IDENTITY() as int)";

        var notificationId = await db.QuerySingleAsync<int>(sql,
            new
            {
                Name = notificationItem.Name, NotificationTypeId = notificationItem.NotificationTypeId,
                Description = notificationItem.Description
            },
            commandType: CommandType.Text);
        return notificationId;
    }

    private async Task DeleteNotificationItemFromChilds(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlEmailSmtp = @"DELETE FROM [NotificationEmailSmtp] WHERE NotificationId = @Id";
        string sqlTeams = @"DELETE FROM [NotificationTeams] WHERE NotificationId = @Id";
        string sqlSlack = @"DELETE FROM [NotificationSlack] WHERE NotificationId = @Id";
        string sqlTelegram = @"DELETE FROM [NotificationTelegram] WHERE NotificationId = @Id";

        await db.ExecuteAsync(sqlEmailSmtp, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlTeams, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlSlack, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlTelegram, new { Id = id }, commandType: CommandType.Text);
    }

    private async Task<List<NotificationEmail>> SelectNotificationEmailList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT NotificationId, FromEmail, ToEmail, HostName, Port, Username, Password, ToCCEmail, ToBCCEmail, EnableSSL, Subject, Body, IsHtmlBody FROM [NotificationEmailSmtp]";

        var resultList = await db.QueryAsync<NotificationEmail>(sql, commandType: CommandType.Text);
        return resultList.ToList();
    }

    private async Task<List<NotificationTeams>> SelectNotificationTeamsList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT NotificationId, WebHookUrl FROM [NotificationTeams]";

        var resultList = await db.QueryAsync<NotificationTeams>(sql, commandType: CommandType.Text);
        return resultList.ToList();
    }

    private async Task<List<NotificationSlack>> SelectNotificationSlackList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT NotificationId, WebHookUrl, Channel FROM [NotificationSlack]";

        var resultList = await db.QueryAsync<NotificationSlack>(sql, commandType: CommandType.Text);
        return resultList.ToList();
    }


    private async Task<List<NotificationTelegram>> SelectNotificationTelegramList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT NotificationId, ChatId, TelegramBotToken FROM [NotificationTelegram]";

        var resultList = await db.QueryAsync<NotificationTelegram>(sql, commandType: CommandType.Text);
        return resultList.ToList();
    }
}