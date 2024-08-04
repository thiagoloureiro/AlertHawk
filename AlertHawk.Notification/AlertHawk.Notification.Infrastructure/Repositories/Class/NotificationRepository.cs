using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Utils;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

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
        string sql = "SELECT Id, MonitorGroupId, Name, Description, NotificationTypeId FROM [NotificationItem]";

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

    public async Task InsertNotificationItemEmailSmtp(NotificationItem? notificationItem)
    {
        if (notificationItem != null)
        {
            var notificationId = notificationItem.Id;

            if (string.IsNullOrEmpty(notificationItem.NotificationEmail?.ToCCEmail))
            {
                if (notificationItem.NotificationEmail != null) notificationItem.NotificationEmail.ToCCEmail = null;
            }

            if (string.IsNullOrEmpty(notificationItem.NotificationEmail?.ToBCCEmail))
            {
                if (notificationItem.NotificationEmail != null) notificationItem.NotificationEmail.ToBCCEmail = null;
            }

            if (notificationItem.NotificationEmail != null)
            {
                notificationItem.NotificationEmail.Password =
                    AesEncryption.EncryptString(notificationItem.NotificationEmail?.Password);

                await using var db = new SqlConnection(_connstring);
                string sqlDetails =
                    @"INSERT INTO [NotificationEmailSmtp] (NotificationId, FromEmail, ToEmail, HostName, Port, Username, Password, ToCCEmail, ToBCCEmail, EnableSSL, Subject, Body, IsHtmlBody)
        VALUES (@notificationId, @FromEmail, @ToEmail, @HostName, @Port, @Username, @Password, @ToCCEmail, @ToBCCEmail, @EnableSSL, @Subject, @Body, @IsHtmlBody)";

                await db.ExecuteAsync(sqlDetails, new
                {
                    notificationId,
                    notificationItem.NotificationEmail?.FromEmail,
                    notificationItem.NotificationEmail?.ToEmail,
                    notificationItem.NotificationEmail?.Hostname,
                    notificationItem.NotificationEmail?.Port,
                    notificationItem.NotificationEmail?.Username,
                    notificationItem.NotificationEmail?.Password,
                    notificationItem.NotificationEmail?.ToCCEmail,
                    notificationItem.NotificationEmail?.ToBCCEmail,
                    notificationItem.NotificationEmail?.EnableSsl,
                    notificationItem.NotificationEmail?.Subject,
                    notificationItem.NotificationEmail?.Body,
                    notificationItem.NotificationEmail?.IsHtmlBody
                }, commandType: CommandType.Text);
            }
        }
    }

    public async Task UpdateNotificationItem(NotificationItem? notificationItem)
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

    public async Task InsertNotificationItemMsTeams(NotificationItem? notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationTeams] (NotificationId, WebHookUrl) VALUES (@notificationId, @WebHookUrl)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId = notificationItem.Id,
            notificationItem.NotificationTeams?.WebHookUrl
        }, commandType: CommandType.Text);
    }

    public async Task InsertNotificationItemTelegram(NotificationItem? notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationTelegram] (NotificationId, ChatId, TelegramBotToken) VALUES (@notificationId, @ChatId, @TelegramBotToken)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId = notificationItem.Id,
            notificationItem.NotificationTelegram?.ChatId,
            notificationItem.NotificationTelegram?.TelegramBotToken,
        }, commandType: CommandType.Text);
    }

    public async Task InsertNotificationItemSlack(NotificationItem? notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationSlack] (NotificationId, WebHookUrl, Channel) VALUES (@notificationId, @WebHookUrl, @ChannelName)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId = notificationItem?.Id,
            notificationItem?.NotificationSlack?.WebHookUrl,
            ChannelName = notificationItem?.NotificationSlack?.Channel
        }, commandType: CommandType.Text);
    }

    public async Task InsertNotificationItemWebHook(NotificationItem? notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlDetails =
            @"INSERT INTO [NotificationWebHook] (NotificationId, Message, WebHookUrl, Body, HeadersJson) VALUES (@notificationId, @Message, @WebHookUrl, @Body, @HeadersJson)";

        await db.ExecuteAsync(sqlDetails, new
        {
            notificationId = notificationItem.Id,
            notificationItem.NotificationWebHook?.Message,
            notificationItem.NotificationWebHook?.WebHookUrl,
            notificationItem.NotificationWebHook?.Body,
            notificationItem.NotificationWebHook?.HeadersJson
        }, commandType: CommandType.Text);
    }

    public async Task<NotificationItem?> SelectNotificationItemById(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT Id, MonitorGroupId, Name, Description, NotificationTypeId FROM [NotificationItem]";

        var notificationItemList = await db.QueryAsync<NotificationItem>(sql, commandType: CommandType.Text);
        var notificationItem = notificationItemList.FirstOrDefault(x => x.Id == id);

        var notificationEmailList = await SelectNotificationEmailList();
        var notificationTeamsList = await SelectNotificationTeamsList();
        var notificationSlackList = await SelectNotificationSlackList();
        var notificationTelegramList = await SelectNotificationTelegramList();
        var notificationWebHookList = await SelectNotificationWebHookList();

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

            case 5: // WebHook
                notificationItem.NotificationWebHook =
                    notificationWebHookList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
                break;
        }

        return notificationItem;
    }

    public async Task<IEnumerable<NotificationItem?>> SelectNotificationItemList(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            "SELECT Id, MonitorGroupId, Name, Description, NotificationTypeId FROM [NotificationItem] WHERE id IN @ids";

        IEnumerable<NotificationItem?> notificationItemList =
            await db.QueryAsync<NotificationItem>(sql, new { ids }, commandType: CommandType.Text);

        var notificationEmailList = await SelectNotificationEmailList();
        var notificationTeamsList = await SelectNotificationTeamsList();
        var notificationSlackList = await SelectNotificationSlackList();
        var notificationTelegramList = await SelectNotificationTelegramList();
        var notificationWebHookList = await SelectNotificationWebHookList();

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

                case 5: // WebHook
                    notificationItem.NotificationWebHook =
                        notificationWebHookList.SingleOrDefault(x => x.NotificationId == notificationItem.Id);
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

    public async Task<IEnumerable<NotificationItem?>> SelectNotificationItemByMonitorGroupId(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            "SELECT NI.Id, NI.MonitorGroupId, NI.Name, NI.Description, NI.NotificationTypeId FROM [NotificationItem] NI " +
            "INNER JOIN NotificationMonitorGroup NMG on NMG.NotificationId = NI.Id WHERE NMG.MonitorGroupId = @id";

        return await db.QueryAsync<NotificationItem>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task InsertNotificationLog(NotificationLog notificationLog)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "INSERT INTO [NotificationLog] (TimeStamp, NotificationTypeId, Message) VALUES (@TimeStamp, @NotificationTypeId, @Message)";
        await db.ExecuteAsync(sql, new { notificationLog.TimeStamp, notificationLog.NotificationTypeId, notificationLog.Message }, commandType: CommandType.Text);
    }

    public async Task<long> GetNotificationLogCount()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT COUNT(*) FROM [NotificationLog]";
        var result = await db.ExecuteScalarAsync<long>(sql, commandType: CommandType.Text);
        return result;
    }

    public async Task<int> InsertNotificationItem(NotificationItem? notificationItem)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"INSERT INTO [NotificationItem] (Name, MonitorGroupId, NotificationTypeId, Description) VALUES (@Name, @MonitorGroupId, @NotificationTypeId, @Description); SELECT CAST(SCOPE_IDENTITY() as int)";

        var notificationId = await db.QuerySingleAsync<int>(sql,
            new
            {
                Name = notificationItem.Name,
                NotificationTypeId = notificationItem.NotificationTypeId,
                Description = notificationItem.Description,
                MonitorGroupId = notificationItem.MonitorGroupId
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
        string sqlWebHook = @"DELETE FROM [NotificationWebHook] WHERE NotificationId = @Id";

        await db.ExecuteAsync(sqlEmailSmtp, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlTeams, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlSlack, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlTelegram, new { Id = id }, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlWebHook, new { Id = id }, commandType: CommandType.Text);
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

    private async Task<List<NotificationWebHook>> SelectNotificationWebHookList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT NotificationId, Message, WebHookUrl, Body, HeadersJson  FROM [NotificationWebHook]";

        var resultList = await db.QueryAsync<NotificationWebHook>(sql, commandType: CommandType.Text);
        return resultList.ToList();
    }
}