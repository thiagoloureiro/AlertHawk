# Notification

The **AlertHawk.Notification** service consumes alert events from the Monitoring service (via RabbitMQ or Azure Service Bus), resolves notification targets (email, Slack, Teams, Telegram, webhook, push), and sends notifications through the configured channels.

All API routes are prefixed with **`/notification`** (e.g. `/notification/api/Notification/SelectNotificationItemList`). Most endpoints require **JWT Bearer** or **Azure AD**; exceptions are noted below.

---

## Overview

- **Message queue**: Consumes alert messages from Monitoring (RabbitMQ or Service Bus)
- **Notification types**: Email (SMTP), Slack, Microsoft Teams, Telegram, Webhook (HTTP), Push (Pushy)
- **Notification items**: Stored in SQL; each item holds credentials/config per channel (e.g. Slack webhook URL, email SMTP)
- **AES encryption**: Sensitive fields (e.g. email password) are encrypted using `AesKey` and `AesIV`

---

## Environment Variables

Configuration can be set in `appsettings.json` or via environment variables (e.g. Helm chart under `notification.env`). Use `__` for nested keys.

### General

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment (`Development`, `Production`) |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | .NET globalization (`false` recommended) |
| `basePath` | Base path for Swagger (dev) |

### Database

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | **Required.** SQL Server connection string |

### Message queue (RabbitMQ or Azure Service Bus)

| Variable | Description |
|----------|-------------|
| `RabbitMq__Host` | RabbitMQ host |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `QueueType` | `RABBITMQ` or `SERVICEBUS` |
| `ServiceBus__ConnectionString` | Azure Service Bus connection string |
| `ServiceBus__QueueName` | Service Bus queue name (e.g. `notifications`) |

### Authentication API

| Variable | Description |
|----------|-------------|
| `AUTH_API_URL` | **Required.** Base URL of the Authentication API (used to resolve user/device tokens for push, etc.) |

### Encryption (notification item secrets)

| Variable | Description |
|----------|-------------|
| `AesKey` | AES key (hex string) for encrypting/decrypting notification item credentials |
| `AesIV` | AES IV (hex string) |

### Slack (default / fallback)

| Variable | Description |
|----------|-------------|
| `slack-webhookurl` | Default Slack webhook URL (optional; per-item config is stored in DB) |

### Push (Pushy)

| Variable | Description |
|----------|-------------|
| `PUSHY_API_KEY` | Pushy API key for push notifications |

### Azure AD / JWT

| Variable | Description |
|----------|-------------|
| `AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__ClientSecret`, `AzureAd__Instance` | Azure AD (for token validation) |
| `Jwt__Key`, `Jwt__Issuers`, `Jwt__Audiences` | JWT validation |

### Cache

| Variable | Description |
|----------|-------------|
| `CacheSettings__CacheProvider` | Cache provider (e.g. `MemoryCache`) |

### Sentry and logging

| Variable | Description |
|----------|-------------|
| `Sentry__Enabled`, `Sentry__Dsn`, `Sentry__Environment` | Sentry error tracking |
| `Logging__LogLevel__Default` | Default log level |
| `Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter` | Identity logger level |

### Swagger (development)

| Variable | Description |
|----------|-------------|
| `SwaggerUICredentials__username`, `SwaggerUICredentials__password` | Basic auth for Swagger UI |

---

## API Controllers

Base path: **`/notification/api`**. Auth: **JWT Bearer** or **Azure AD** unless noted.

### Notification — `/notification/api/Notification/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/SendManualNotification` | Bearer | Send manual notification (body: NotificationSend; supports email, push, etc.) |
| `POST` | `/CreateNotificationItem` | Bearer | Create notification item (channel config) |
| `PUT` | `/UpdateNotificationItem` | Bearer | Update notification item |
| `DELETE` | `/DeleteNotificationItem` | Bearer | Delete notification item (query: id) |
| `GET` | `/SelectNotificationItemList` | Bearer | List notification items for current user |
| `POST` | `/SelectNotificationItemListByIds` | Bearer | List notification items by IDs (body: list of ids) |
| `GET` | `/SelectNotificationItemById/{id}` | Bearer | Get notification item by ID |
| `GET` | `/SelectNotificationByMonitorGroup/{id}` | Bearer | Notification items by monitor group ID |
| `GET` | `/GetNotificationCount` | Bearer | Notification log count |
| `GET` | `/ClearNotificationStatistics` | Bearer | Clear notification statistics |

### NotificationType — `/notification/api/NotificationType/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/GetNotificationType` | Bearer | List notification types (cached) |
| `GET` | `/GetNotificationType/{id}` | Bearer | Notification type by ID |
| `POST` | `/InsertNotificationType` | Bearer | Create notification type |
| `PUT` | `/UpdateNotificationType` | Bearer | Update notification type |
| `DELETE` | `/DeleteNotificationType` | Bearer | Delete notification type (query: id) |

### Version — `/notification/api/Version`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/` | None | API version |

---

## Supported Channels

| Type | Description |
|------|-------------|
| **Email** | SMTP; credentials stored in notification item (password encrypted with AesKey/AesIV) |
| **Slack** | Webhook URL per item or `slack-webhookurl` env |
| **Microsoft Teams** | Incoming webhook URL per item |
| **Telegram** | Bot token and chat ID per item |
| **Webhook** | Generic HTTP URL and optional headers per item |
| **Push** | Pushy; uses `PUSHY_API_KEY` and device tokens from Authentication API |

---

## Helm Chart Reference

In the [Helm chart](/helm/), Notification is configured under the `notification` section. Set `notification.env` with the variables above (e.g. `ConnectionStrings__SqlConnectionString`, `RabbitMq__*` or `ServiceBus__*`, `AUTH_API_URL`, `AesKey`, `AesIV`, `PUSHY_API_KEY`, Azure AD, JWT, etc.). See [Environment variables](/helm/environment-variables#notification) in the Helm docs.
