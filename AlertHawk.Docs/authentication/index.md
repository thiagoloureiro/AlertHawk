# Authentication

The **AlertHawk.Authentication** service handles user identity and access for the AlertHawk platform: user sign-in (e.g. Microsoft Account), JWT and Azure AD token issuance/validation, and integration with other AlertHawk services.

All API routes are prefixed with **`/auth`** (e.g. `/auth/api/User/GetAll`).

---

## Environment Variables

These variables are used when running the Authentication service (e.g. via Helm chart under `auth.env`). Configuration can be set in `appsettings.json` or via environment variables (double underscore `__` in Helm for nested keys).

### General

| Variable | Description | Example / Notes |
|----------|-------------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development`, `Production` |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | .NET globalization | `false` recommended |
| `basePath` | Base path for Swagger (dev) | Optional, e.g. `/auth` |

### Database

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__SqlConnectionString` | SQL Server connection string for user and auth data |

### Azure AD (Microsoft Identity)

| Variable | Description |
|----------|-------------|
| `AzureAd__ClientId` | Azure AD application (client) ID |
| `AzureAd__TenantId` | Azure AD tenant ID |
| `AzureAd__ClientSecret` | Azure AD client secret |
| `AzureAd__Instance` | Azure AD instance URL (e.g. `https://login.microsoftonline.com/`) |
| `AzureAd__CallbackPath` | OIDC callback path | `/signin-oidc` |

### Downstream API (Microsoft Graph)

| Variable | Description |
|----------|-------------|
| `DownstreamApi__BaseUrl` | Microsoft Graph base URL | e.g. `https://graph.microsoft.com/beta` |
| `DownstreamApi__Scopes` | Scopes to request | e.g. `User.Read` |

### JWT (token generation and validation)

| Variable | Description |
|----------|-------------|
| `Jwt__Key` | Secret key used to sign/validate JWT tokens |
| `Jwt__Issuers` | Comma-separated list of valid issuers |
| `Jwt__Audiences` | Comma-separated list of valid audiences |

### Mobile / API key

| Variable | Description |
|----------|-------------|
| `MOBILE_API_KEY` | API key required for mobile Azure auth (`POST /auth/api/Auth/azure`) |

### SMTP (password reset / email)

| Variable | Description |
|----------|-------------|
| `smtpHost` | SMTP server host |
| `smtpPort` | SMTP port (e.g. 587) |
| `smtpUsername` | SMTP username |
| `smtpPassword` | SMTP password |
| `smtpFrom` | From address for outgoing email |
| `enableSsl` | Use SSL for SMTP | `true` / `false` |

### Swagger UI (development)

| Variable | Description |
|----------|-------------|
| `SwaggerUICredentials__username` | Basic auth username for Swagger UI |
| `SwaggerUICredentials__password` | Basic auth password for Swagger UI |

### Caching

| Variable | Description |
|----------|-------------|
| `CacheSettings__CacheProvider` | Cache implementation | e.g. `MemoryCache` |

### Sentry

| Variable | Description |
|----------|-------------|
| `Sentry__Enabled` | Enable Sentry error reporting | `true` / `false` |
| `Sentry__Dsn` | Sentry DSN URL |
| `Sentry__Environment` | Environment name sent to Sentry |

### Logging

| Variable | Description |
|----------|-------------|
| `Logging__LogLevel__Default` | Default log level | e.g. `Warning` |
| `Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter` | Identity logger level | e.g. `Critical` |

### Feature flags / behavior

| Variable | Description | Default |
|----------|-------------|--------|
| `ENABLED_LOGIN_AUTH` | Enable username/password login and related endpoints | `true`; set to `false` to disable login, create, reset password, update password |
| `DEMO_MODE` | When `true`, new users from Azure mobile auth get a default monitor group (e.g. group 24) | `false` |
| `BLOCKED_DOMAINS` | Comma-separated email domains; users whose UPN/email ends with `@<domain>` get 403 | Optional |

---

## API Controllers

All controllers live under the base path **`/auth/api`**. Authentication uses **JWT Bearer** or **Azure AD**; most endpoints require authorization unless marked otherwise.

### Auth — `POST /auth/api/Auth/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/auth/api/Auth/azure` | None | Get JWT for mobile app; body: `{ "email", "apiKey" }`; `apiKey` must match `MOBILE_API_KEY`. Creates user if not exists. |
| `POST` | `/auth/api/Auth/refreshToken` | Bearer | Refresh JWT using current token. |
| `POST` | `/auth/api/Auth/login` | None | Username/password login; returns JWT. Disabled when `ENABLED_LOGIN_AUTH=false`. |

### User — `POST|GET|PUT|DELETE /auth/api/User/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/auth/api/User/create` | None | Create user (email/password). Disabled when `ENABLED_LOGIN_AUTH=false`. |
| `DELETE` | `/auth/api/User/delete/{userId}` | Admin | Delete user by ID. |
| `DELETE` | `/auth/api/User/delete` | Bearer | Delete current user (by token). |
| `PUT` | `/auth/api/User/update` | Admin | Update user. |
| `POST` | `/auth/api/User/resetPassword/{email}` | None | Send password reset email. Disabled when `ENABLED_LOGIN_AUTH=false`. |
| `POST` | `/auth/api/User/updatePassword` | Bearer | Change password (current + new). Disabled when `ENABLED_LOGIN_AUTH=false`. |
| `GET` | `/auth/api/User/GetAll` | Admin | Get all users. |
| `GET` | `/auth/api/User/GetAllByGroupId/{groupId}` | Bearer | Get users by group ID. |
| `GET` | `/auth/api/User/GetById/{userId}` | Bearer | Get user by ID. |
| `GET` | `/auth/api/User/GetByEmail/{userEmail}` | Bearer | Get user by email. |
| `GET` | `/auth/api/User/GetByUserName/{userName}` | Bearer | Get user by username. |
| `GET` | `/auth/api/User/{email}` | Bearer | Get user by email; creates from Azure AD if not exists. |
| `GET` | `/auth/api/User/GetUserCount` | Bearer | Get total user count. |
| `GET` | `/auth/api/User/GetUserDetailsByToken` | Bearer | Get current user from token. |
| `POST` | `/auth/api/User/UpdateUserDeviceToken` | Bearer | Update device token for push. Body: `{ "deviceToken" }`. |
| `GET` | `/auth/api/User/GetUserDeviceTokenList` | Bearer | Get device tokens for current user. |
| `GET` | `/auth/api/User/GetUserDeviceTokenListByUserId/{userId}` | Bearer | Get device tokens by user ID. |
| `GET` | `/auth/api/User/GetUserDeviceTokenListByGroupId/{groupId}` | None | Get device tokens by group ID. |

### UserAction — `POST|GET /auth/api/UserAction/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/auth/api/UserAction/create` | Bearer | Create user action (body: action payload); `userId` set from token. |
| `GET` | `/auth/api/UserAction` | Bearer | Get list of user actions. |

### UserClusters — `POST|GET /auth/api/UserClusters/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/auth/api/UserClusters` | Admin | Add a cluster to a user. |
| `POST` | `/auth/api/UserClusters/CreateOrUpdate` | Admin | Add or update multiple clusters; body: `{ "userId", "clusters": [] }`. Empty list removes all. |
| `GET` | `/auth/api/UserClusters/GetAllByUserId/{userId}` | Bearer | Get clusters by user ID (own user or admin). |

### UsersMonitorGroup — `POST|GET|DELETE /auth/api/UsersMonitorGroup/*`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `POST` | `/auth/api/UsersMonitorGroup/AssignUserToGroup` | Bearer | Assign current user to a monitor group. |
| `POST` | `/auth/api/UsersMonitorGroup/create` | Admin | Assign users to groups (list). |
| `GET` | `/auth/api/UsersMonitorGroup/GetAll` | Bearer | Get all monitor group IDs for current user. |
| `GET` | `/auth/api/UsersMonitorGroup/GetAllByUserId/{userId}` | Admin | Get monitor groups by user ID. |
| `DELETE` | `/auth/api/UsersMonitorGroup/{groupMonitorId}` | Admin | Delete all user-group relationships for a group. |

### Version — `GET /auth/api/Version`

| Method | Route | Auth | Description |
|--------|--------|------|-------------|
| `GET` | `/auth/api/Version` | None | Return API version string. |

---

## Helm chart reference

In the [AlertHawk Helm chart](https://github.com/thiagoloureiro/AlertHawk.Chart), Authentication is configured under the `auth` section. Example from `values.yaml`:

```yaml
auth:
  replicas: 1
  env:
    ASPNETCORE_ENVIRONMENT: Development
    ConnectionStrings__SqlConnectionString: your-connection-string
    Sentry__Enabled: false
    Sentry__Dsn: sentry-dsn-url
    Sentry__Environment: Local
    SwaggerUICredentials__username: admin
    SwaggerUICredentials__password: admin
    CacheSettings__CacheProvider: MemoryCache
    AzureAd__ClientId: clientid
    AzureAd__TenantId: tenantid
    AzureAd__ClientSecret: secret
    AzureAd__Instance: instance
    AzureAd__CallbackPath: /signin-oidc
    DownstreamApi__BaseUrl: https://graph.microsoft.com/beta
    DownstreamApi__Scopes: User.Read
    smtpHost: smtp-host
    smtpPort: smtp-port
    smtpUsername: smtp-user
    smtpPassword: smtp-pass
    smtpFrom: smtp-from
    enableSsl: true
    Jwt__Key: jwt-key
    Jwt__Issuers: issuers
    Jwt__Audiences: audiences
    Logging__LogLevel__Default: Warning
    Logging__LogLevel__Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter: Critical
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: false
    MOBILE_API_KEY: your_auth_api_key
```

Adjust values (especially secrets and connection strings) for your environment.
