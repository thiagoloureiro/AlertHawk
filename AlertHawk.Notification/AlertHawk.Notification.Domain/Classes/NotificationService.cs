﻿using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Notification.Domain.Entities;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;
using AlertHawk.Notification.Domain.Utils;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace AlertHawk.Notification.Domain.Classes
{
    public class NotificationService : INotificationService
    {
        private readonly IMailNotifier _mailNotifier;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ITeamsNotifier _teamsNotifier;
        private readonly ITelegramNotifier _telegramNotifier;
        private readonly INotificationRepository _notificationRepository;
        private readonly IWebHookNotifier _webHookNotifier;
        private readonly IPushNotifier _pushNotifier;

        public NotificationService(IMailNotifier mailNotifier, ISlackNotifier slackNotifier,
            ITeamsNotifier teamsNotifier, ITelegramNotifier telegramNotifier,
            INotificationRepository notificationRepository, IWebHookNotifier webHookNotifier, IPushNotifier pushNotifier)
        {
            _mailNotifier = mailNotifier;
            _slackNotifier = slackNotifier;
            _teamsNotifier = teamsNotifier;
            _telegramNotifier = telegramNotifier;
            _notificationRepository = notificationRepository;
            _webHookNotifier = webHookNotifier;
            _pushNotifier = pushNotifier;
        }

        public async Task<bool> Send(NotificationSend notificationSend)
        {
            try
            {
                var notificationLog = new NotificationLog
                {
                    TimeStamp = DateTime.UtcNow,
                    NotificationTypeId = notificationSend.NotificationTypeId,
                };

                switch (notificationSend.NotificationTypeId)
                {
                    case 1: // Email SMTP
                        notificationSend.NotificationEmail.Password =
                            AesEncryption.DecryptString(notificationSend.NotificationEmail.Password);

                        notificationSend.NotificationEmail.Body += notificationSend.Message;
                        notificationLog.Message =
                            $"Subject:{notificationSend.NotificationEmail.Subject} Body:{notificationSend.NotificationEmail.Body} Email: {notificationSend.NotificationEmail.ToEmail}, CCEmail: {notificationSend.NotificationEmail.ToCCEmail}, BCCEmail:{notificationSend.NotificationEmail.ToBCCEmail}";
                        var result = await _mailNotifier.Send(notificationSend.NotificationEmail);
                        await InsertNotificationLog(notificationLog);
                        return result;

                    case 2: // MS Teams
                        notificationLog.Message = $"Teams Message:{notificationSend.Message};";
                        await _teamsNotifier.SendNotification(notificationSend.Message,
                            notificationSend.NotificationTeams.WebHookUrl);
                        await InsertNotificationLog(notificationLog);
                        return true;

                    case 3: // Telegram
                        notificationLog.Message =
                            $"Telegram Message:{notificationSend.Message} ChatId: {notificationSend.NotificationTelegram.ChatId}";
                        await _telegramNotifier.SendNotification(notificationSend.NotificationTelegram.ChatId,
                            notificationSend.Message,
                            notificationSend.NotificationTelegram.TelegramBotToken);
                        await InsertNotificationLog(notificationLog);
                        return true;

                    case 4: // Slack
                        notificationLog.Message =
                            $"Telegram Message:{notificationSend.Message} ChatId: {notificationSend.NotificationSlack.Channel}";
                        await _slackNotifier.SendNotification(notificationSend.NotificationSlack.Channel,
                            notificationSend.Message, notificationSend.NotificationSlack.WebHookUrl);
                        await InsertNotificationLog(notificationLog);
                        return true;

                    case 5: // WebHook
                        notificationLog.Message =
                            $"Telegram Message:{notificationSend.Message} WebHookUrl: {notificationSend.NotificationWebHook.WebHookUrl}";
                        
                        JsonUtils.ConvertJsonToTuple(notificationSend.NotificationWebHook);

                        await _webHookNotifier.SendNotification(notificationSend, notificationSend.NotificationWebHook);

                        await InsertNotificationLog(notificationLog);
                        return true;

                    case 6: // Push
                        notificationLog.Message =
                            $"Push Notification Message:{notificationSend.Message} To Device: {notificationSend.NotificationPush.PushNotificationBody.to}";
                        await _pushNotifier.SendNotification(notificationSend.Message,
                            notificationSend.NotificationPush);
                        return true;
                    default:
                        Console.WriteLine($"Not found NotificationTypeId: {notificationSend.NotificationTypeId}");
                        break;
                }

                return false;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return false;
            }
        }

        public async Task InsertNotificationItem(NotificationItem? notificationItem)
        {
            if (notificationItem != null)
            {
                notificationItem.Name = notificationItem.Name?.TrimStart();
                notificationItem.Name = notificationItem.Name?.TrimEnd();

                if (notificationItem.Id == 0)
                {
                    notificationItem.Id = await _notificationRepository.InsertNotificationItem(notificationItem);
                }

                switch (notificationItem.NotificationTypeId)
                {
                    case 1: // Email SMTP
                        await _notificationRepository.InsertNotificationItemEmailSmtp(notificationItem);
                        break;

                    case 2: // MS Teams
                        await _notificationRepository.InsertNotificationItemMsTeams(notificationItem);
                        break;

                    case 3: // Telegram
                        await _notificationRepository.InsertNotificationItemTelegram(notificationItem);
                        break;

                    case 4: // Slack
                        await _notificationRepository.InsertNotificationItemSlack(notificationItem);
                        break;

                    case 5: // WebHook
                        await _notificationRepository.InsertNotificationItemWebHook(notificationItem);
                        break;
                }
            }
        }

        public async Task UpdateNotificationItem(NotificationItem? notificationItem)
        {
            if (notificationItem != null)
            {
                notificationItem.Name = notificationItem.Name?.TrimStart();
                notificationItem.Name = notificationItem.Name?.TrimEnd();

                await _notificationRepository.UpdateNotificationItem(notificationItem);
                await InsertNotificationItem(notificationItem);
            }
        }

        public async Task DeleteNotificationItem(int id)
        {
            await _notificationRepository.DeleteNotificationItem(id);
        }

        public async Task<IEnumerable<NotificationItem>> SelectNotificationItemList(string jwtToken)
        {
            var groupIds = await GetUserGroupMonitorListIds(jwtToken);
            var items = await _notificationRepository.SelectNotificationItemList();
            return items.Where(x => groupIds != null && groupIds.Contains(x.MonitorGroupId)).ToList();
        }

        public async Task<IEnumerable<NotificationItem?>> SelectNotificationItemList(List<int> ids)
        {
            return await _notificationRepository.SelectNotificationItemList(ids);
        }

        public async Task<NotificationItem?> SelectNotificationItemById(int id)
        {
            return await _notificationRepository.SelectNotificationItemById(id);
        }

        public async Task<List<int>?> GetUserGroupMonitorListIds(string token)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL");

            var content = await client.GetAsync($"{authApi}api/UsersMonitorGroup/GetAll");
            var result = await content.Content.ReadAsStringAsync();
            var groupMonitorIds = JsonConvert.DeserializeObject<List<UsersMonitorGroup>>(result);
            var listGroupMonitorIds = groupMonitorIds?.Select(x => x.GroupMonitorId).ToList();
            return listGroupMonitorIds;
        }

        public async Task<IEnumerable<NotificationItem?>> SelectNotificationItemByMonitorGroupId(int id)
        {
            return await _notificationRepository.SelectNotificationItemByMonitorGroupId(id);
        }

        public async Task InsertNotificationLog(NotificationLog notificationLog)
        {
            await _notificationRepository.InsertNotificationLog(notificationLog);
        }

        public async Task<long> GetNotificationLogCount()
        {
            return await _notificationRepository.GetNotificationLogCount();
        }

        public async Task ClearNotificationStatistics()
        {
            await _notificationRepository.ClearNotificationStatistics();
        }

        public async Task<IEnumerable<string>?> GetDeviceTokenList(int monitorGroupId)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL");
            var content = await client.GetAsync($"{authApi}api/User/GetUserDeviceTokenListByGroupId/{monitorGroupId}");
            var userTokenStr = await content.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(userTokenStr);
        }
    }
}