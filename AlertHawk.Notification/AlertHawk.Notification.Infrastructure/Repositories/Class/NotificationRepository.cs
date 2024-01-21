using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Notification.Infrastructure.Repositories.Class
{
    [ExcludeFromCodeCoverage]
    public class NotificationTypeRepository : RepositoryBase, INotificationTypeRepository
    {
        private readonly string _connstring;

        public NotificationTypeRepository(IConfiguration configuration) : base(configuration)
        {
            _connstring = GetConnectionString();
        }

        public async Task<IEnumerable<NotificationType>> SelectNotificationType()
        {
            // Select
            await using var db = new SqlConnection(_connstring);
            string sql = @"SELECT Id, Name, Description FROM [NotificationType]";

            return await db.QueryAsync<NotificationType>(sql, commandType: CommandType.Text);
        }

        public async Task<NotificationType?> SelectNotificationTypeById(int id)
        {
            // Select
            await using var db = new SqlConnection(_connstring);
            string sql = @"SELECT Id, Name, Description FROM [NotificationType] WHERE Id = @id";

            return await db.QueryFirstOrDefaultAsync<NotificationType>(sql, new { id }, commandType: CommandType.Text);
        }

        public async Task<NotificationType?> SelectNotificationTypeByName(string name)
        {
            // Select
            await using var db = new SqlConnection(_connstring);
            string sql = @"SELECT Id, Name, Description FROM [NotificationType] WHERE Name = @name";

            return await db.QueryFirstOrDefaultAsync<NotificationType>(sql, new { name }, commandType: CommandType.Text);
        }

        public async Task InsertNotificationType(NotificationType notificationtype)
        {
            // Insert
            await using var db = new SqlConnection(_connstring);
            string sql = @"INSERT INTO [NotificationType] (Name, Description) VALUES (@Name, @Description)";

            await db.ExecuteAsync(sql, new { Name = notificationtype.Name, Description = notificationtype.Description }, commandType: CommandType.Text);
        }

        public async Task UpdateNotificationType(NotificationType notificationtype)
        {
            // Update
            await using var db = new SqlConnection(_connstring);
            string sql = @"UPDATE [NotificationType] SET Name = @Name, Description = @Description WHERE Id = @Id";

            await db.ExecuteAsync(sql, new { Id = notificationtype.Id, Name = notificationtype.Name, Description = notificationtype.Description }, commandType: CommandType.Text);
        }

        public async Task DeleteNotificationType(int id)
        {
            // Delete
            await using var db = new SqlConnection(_connstring);
            string sql = @"DELETE FROM [NotificationType] WHERE Id = @Id";

            await db.ExecuteAsync(sql, new { id }, commandType: CommandType.Text);
        }
    }
}