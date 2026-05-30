using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Configuration;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class SecretsRunnerTests
{
    private readonly IAzureAppSecretsFetcher _azureAppSecretsFetcher;
    private readonly IAzureAppSecretRepository _azureAppSecretRepository;
    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly SecretsRunner _secretsRunner;

    private const int MonitorId = 42;
    private static readonly Guid SecretKeyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SecretsRunnerTests()
    {
        _azureAppSecretsFetcher = Substitute.For<IAzureAppSecretsFetcher>();
        _azureAppSecretRepository = Substitute.For<IAzureAppSecretRepository>();
        _monitorRepository = Substitute.For<IMonitorRepository>();
        _notificationProducer = Substitute.For<INotificationProducer>();
        _monitorAlertRepository = Substitute.For<IMonitorAlertRepository>();
        _monitorHistoryRepository = Substitute.For<IMonitorHistoryRepository>();
        _systemConfigurationRepository = Substitute.For<ISystemConfigurationRepository>();
        _settingsProvider = Substitute.For<IAzureSecretsSettingsProvider>();

        _systemConfigurationRepository.IsMonitorExecutionDisabled().Returns(Task.FromResult(false));
        _azureAppSecretRepository.GetAllAsync().Returns(Task.FromResult(Enumerable.Empty<AzureAppSecret>()));

        _secretsRunner = CreateRunner(new AzureSecretsOptions
        {
            Enabled = true,
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret",
            DaysBeforeExpiryToAlert = 30,
            MonitorId = MonitorId
        });
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenDisabled_DoesNotCallDependencies()
    {
        var runner = CreateRunner(new AzureSecretsOptions { Enabled = false, MonitorId = MonitorId });

        await runner.CheckSecretsAsync();

        await _monitorRepository.DidNotReceive().GetMonitorById(Arg.Any<int>());
        _azureAppSecretsFetcher.DidNotReceive()
            .FetchPasswordCredentialsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenMonitorExecutionDisabled_SkipsCheck()
    {
        _systemConfigurationRepository.IsMonitorExecutionDisabled().Returns(Task.FromResult(true));

        await _secretsRunner.CheckSecretsAsync();

        await _monitorRepository.DidNotReceive().GetMonitorById(Arg.Any<int>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenMonitorIdNotConfigured_SkipsCheck()
    {
        var runner = CreateRunner(new AzureSecretsOptions
        {
            Enabled = true,
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret",
            MonitorId = 0
        });

        await runner.CheckSecretsAsync();

        await _monitorRepository.DidNotReceive().GetMonitorById(Arg.Any<int>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenCredentialsMissing_SkipsCheck()
    {
        var runner = CreateRunner(new AzureSecretsOptions
        {
            Enabled = true,
            MonitorId = MonitorId,
            TenantId = "",
            ClientId = "client",
            ClientSecret = "secret"
        });

        await runner.CheckSecretsAsync();

        await _monitorRepository.DidNotReceive().GetMonitorById(Arg.Any<int>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenMonitorNotFound_SkipsCheck()
    {
        _monitorRepository.GetMonitorById(MonitorId).Returns((Monitor?)null);

        await _secretsRunner.CheckSecretsAsync();

        _azureAppSecretsFetcher.DidNotReceive()
            .FetchPasswordCredentialsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenAllSecretsHealthy_UpdatesStatusAndSavesHistory()
    {
        var monitor = CreateMonitor(status: true);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        SetupFetcher(HealthyCredential(daysUntilExpiry: 90));

        await _secretsRunner.CheckSecretsAsync();

        await _monitorRepository.Received(1).UpdateMonitorStatus(MonitorId, true, 0);
        await _monitorHistoryRepository.Received(1).SaveMonitorHistory(Arg.Is<MonitorHistory>(h =>
            h.MonitorId == MonitorId && h.Status));
        await _notificationProducer.DidNotReceive()
            .HandleFailedSecretsNotifications(Arg.Any<Monitor>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenExpiringSecretAndMonitorWasHealthy_SendsFailedNotification()
    {
        var monitor = CreateMonitor(status: true);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        SetupFetcher(HealthyCredential(daysUntilExpiry: 10));

        await _secretsRunner.CheckSecretsAsync();

        await _monitorRepository.Received(1).UpdateMonitorStatus(MonitorId, false, 0);
        await _notificationProducer.Received(1).HandleFailedSecretsNotifications(
            monitor,
            Arg.Is<string>(m => m.Contains("expiring soon")));
        await _monitorAlertRepository.Received(1).SaveMonitorAlert(
            Arg.Is<MonitorHistory>(h => !h.Status),
            monitor.MonitorEnvironment);
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenMonitorRecovers_SendsSuccessNotification()
    {
        var monitor = CreateMonitor(status: false);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        SetupFetcher(HealthyCredential(daysUntilExpiry: 90));

        await _secretsRunner.CheckSecretsAsync();

        await _monitorRepository.Received(1).UpdateMonitorStatus(MonitorId, true, 0);
        await _notificationProducer.Received(1).HandleSuccessSecretsNotifications(
            monitor,
            Arg.Is<string>(m => m.Contains("within the expiry threshold")));
        await _monitorAlertRepository.Received(1).SaveMonitorAlert(
            Arg.Is<MonitorHistory>(h => h.Status),
            monitor.MonitorEnvironment);
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenNewSecretEntersExpiryWindow_SendsTargetedFailedNotification()
    {
        var monitor = CreateMonitor(status: false);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        _azureAppSecretRepository.GetAllAsync().Returns(Task.FromResult(new[]
        {
            new AzureAppSecret
            {
                ApplicationObjectId = "app-obj-id",
                KeyId = SecretKeyId,
                IsExpiring = false
            }
        }.AsEnumerable()));
        SetupFetcher(HealthyCredential(daysUntilExpiry: 5));

        await _secretsRunner.CheckSecretsAsync();

        await _notificationProducer.Received(1).HandleFailedSecretsNotifications(
            monitor,
            Arg.Is<string>(m => m.Contains("entered the expiry window")));
        await _monitorAlertRepository.DidNotReceive().SaveMonitorAlert(Arg.Any<MonitorHistory>(), Arg.Any<MonitorEnvironment>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenSecretRecoversWhileMonitorHealthy_SendsRecoveryNotification()
    {
        var monitor = CreateMonitor(status: true);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        _azureAppSecretRepository.GetAllAsync().Returns(Task.FromResult(new[]
        {
            new AzureAppSecret
            {
                ApplicationObjectId = "app-obj-id",
                KeyId = SecretKeyId,
                IsExpiring = true
            }
        }.AsEnumerable()));
        SetupFetcher(HealthyCredential(daysUntilExpiry: 90));

        await _secretsRunner.CheckSecretsAsync();

        await _notificationProducer.Received(1).HandleSuccessSecretsNotifications(
            monitor,
            Arg.Is<string>(m => m.Contains("no longer in the expiry window")));
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenFetcherThrows_SendsFailedNotificationIfWasHealthy()
    {
        var monitor = CreateMonitor(status: true);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        _azureAppSecretsFetcher.FetchPasswordCredentialsAsync(Arg.Any<CancellationToken>())
            .Returns(ThrowingAsyncEnumerable());

        await _secretsRunner.CheckSecretsAsync();

        await _monitorRepository.Received(1).UpdateMonitorStatus(MonitorId, false, 0);
        await _notificationProducer.Received(1).HandleFailedSecretsNotifications(
            monitor,
            Arg.Is<string>(m => m.Contains("Graph API error")));
        await _monitorAlertRepository.Received(1).SaveMonitorAlert(
            Arg.Is<MonitorHistory>(h => !h.Status),
            monitor.MonitorEnvironment);
    }

    [Fact]
    public async Task CheckSecretsAsync_UpsertsEachFetchedSecret()
    {
        var monitor = CreateMonitor(status: true);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        SetupFetcher(
            HealthyCredential(daysUntilExpiry: 90),
            HealthyCredential(daysUntilExpiry: 60, keyId: Guid.Parse("22222222-2222-2222-2222-222222222222")));

        await _secretsRunner.CheckSecretsAsync();

        await _azureAppSecretRepository.Received(2).UpsertAsync(Arg.Any<AzureAppSecret>());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void BuildResponseMessage_ReflectsExpiringCount(int expiringCount, bool expectedSucceeded)
    {
        var secrets = Enumerable.Range(0, expiringCount).Select(i => new AzureAppSecret
        {
            ApplicationDisplayName = $"App{i}",
            AppId = $"app-{i}",
            KeyId = Guid.NewGuid(),
            DaysUntilExpiry = 5,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(5)
        }).ToList();

        var message = SecretsRunnerMessages.BuildResponseMessage(secrets, expectedSucceeded);

        if (expectedSucceeded)
        {
            Assert.Contains("within the expiry threshold", message);
        }
        else
        {
            Assert.Contains($"{expiringCount} Azure app secret(s) expiring soon", message);
        }
    }

    private SecretsRunner CreateRunner(AzureSecretsOptions options)
    {
        _settingsProvider.GetSettingsAsync().Returns(Task.FromResult(options));

        return new SecretsRunner(
            _settingsProvider,
            _azureAppSecretsFetcher,
            _azureAppSecretRepository,
            _monitorRepository,
            _notificationProducer,
            _monitorAlertRepository,
            _monitorHistoryRepository,
            _systemConfigurationRepository,
            Substitute.For<ILogger<SecretsRunner>>());
    }

    private static Monitor CreateMonitor(bool status) => new()
    {
        Id = MonitorId,
        Name = "Azure Secrets",
        Status = status,
        MonitorEnvironment = MonitorEnvironment.Production,
        MonitorRegion = MonitorRegion.NorthAmerica
    };

    private static AzurePasswordCredentialInfo HealthyCredential(
        int daysUntilExpiry,
        Guid? keyId = null) => new(
        "app-obj-id",
        "Test App",
        "app-client-id",
        keyId ?? SecretKeyId,
        "Primary secret",
        DateTimeOffset.UtcNow.AddDays(daysUntilExpiry));

    private void SetupFetcher(params AzurePasswordCredentialInfo[] credentials)
    {
        _azureAppSecretsFetcher.FetchPasswordCredentialsAsync(Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(credentials));
    }

    private static async IAsyncEnumerable<AzurePasswordCredentialInfo> ToAsyncEnumerable(
        IEnumerable<AzurePasswordCredentialInfo> items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<AzurePasswordCredentialInfo> ThrowingAsyncEnumerable()
    {
        await Task.Yield();
        throw new InvalidOperationException("Graph API error");
        yield break;
    }
}
