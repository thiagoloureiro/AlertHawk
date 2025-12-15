using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using AlertHawk.Notification.Infrastructure.Helpers;

namespace AlertHawk.Notification.Infrastructure.Repositories.Class
{
    [ExcludeFromCodeCoverage]
    public class NotificationTypeRepository : RepositoryBase, INotificationTypeRepository
    {
        public NotificationTypeRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<IEnumerable<NotificationType>> SelectNotificationType()
        {
            // Select
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("NotificationType", DatabaseProvider);
            string sql = $"SELECT Id, Name, Description FROM {tableName}";

            return await db.QueryAsync<NotificationType>(sql, commandType: CommandType.Text);
        }

        public async Task<NotificationType?> SelectNotificationTypeById(int id)
        {
            // Select
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("NotificationType", DatabaseProvider);
            string sql = $"SELECT Id, Name, Description FROM {tableName} WHERE Id = @id";

            return await db.QueryFirstOrDefaultAsync<NotificationType>(sql, new { id }, commandType: CommandType.Text);
        }

        public async Task<NotificationType?> SelectNotificationTypeByName(string name)
        {
            // Select
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("NotificationType", DatabaseProvider);
            string sql = $"SELECT Id, Name, Description FROM {tableName} WHERE Name = @name";

            return await db.QueryFirstOrDefaultAsync<NotificationType>(sql, new { name }, commandType: CommandType.Text);
        }

        public async Task InsertNotificationType(NotificationType notificationtype)
        {
            // Insert
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("NotificationType", DatabaseProvider);
            string sql = $"INSERT INTO {tableName} (Id, Name, Description) VALUES (@Id, @Name, @Description)";

            await db.ExecuteAsync(sql, new { Id = notificationtype.Id, Name = notificationtype.Name, Description = notificationtype.Description }, commandType: CommandType.Text);
        }

        public async Task UpdateNotificationType(NotificationType notificationtype)
        {
            // Update
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("NotificationType", DatabaseProvider);
            string sql = $"UPDATE {tableName} SET Name = @Name, Description = @Description WHERE Id = @Id";

            await db.ExecuteAsync(sql, new { Id = notificationtype.Id, Name = notificationtype.Name, Description = notificationtype.Description }, commandType: CommandType.Text);
        }

        public async Task DeleteNotificationType(int id)
        {
            // Delete
            using var db = CreateConnection();
            var tableName = Helpers.DatabaseProvider.FormatTableName("NotificationType", DatabaseProvider);
            string sql = $"DELETE FROM {tableName} WHERE Id = @Id";

            await db.ExecuteAsync(sql, new { id }, commandType: CommandType.Text);
        }
    }
}