using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Configuration;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class SecretsRunnerTests
{
    private readonly IAzureAppSecretsFetcher _azureAppSecretsFetcher;
    private readonly IAzureAppSecretRepository _azureAppSecretRepository;
    private readonly IAzureAppRegistrationWatchRepository _watchRepository;
    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly SecretsRunner _secretsRunner;

    private const int MonitorId = 42;
    private const string AppObjectId = "app-obj-id";
    private static readonly Guid SecretKeyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SecretsRunnerTests()
    {
        _azureAppSecretsFetcher = Substitute.For<IAzureAppSecretsFetcher>();
        _azureAppSecretRepository = Substitute.For<IAzureAppSecretRepository>();
        _watchRepository = Substitute.For<IAzureAppRegistrationWatchRepository>();
        _monitorRepository = Substitute.For<IMonitorRepository>();
        _notificationProducer = Substitute.For<INotificationProducer>();
        _monitorAlertRepository = Substitute.For<IMonitorAlertRepository>();
        _monitorHistoryRepository = Substitute.For<IMonitorHistoryRepository>();
        _systemConfigurationRepository = Substitute.For<ISystemConfigurationRepository>();
        _settingsProvider = Substitute.For<IAzureSecretsSettingsProvider>();

        _systemConfigurationRepository.IsMonitorExecutionDisabled().Returns(Task.FromResult(false));
        _azureAppSecretRepository.GetAllAsync().Returns(Task.FromResult(Enumerable.Empty<AzureAppSecret>()));
        SetupRegisteredApps(AppObjectId);

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
            .FetchPasswordCredentialsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenNoRegistrations_SkipsGraphFetch()
    {
        _watchRepository.GetAllAsync().Returns(Task.FromResult(Enumerable.Empty<AzureAppRegistrationWatch>()));

        await _secretsRunner.CheckSecretsAsync();

        _azureAppSecretsFetcher.DidNotReceive()
            .FetchPasswordCredentialsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        await _azureAppSecretRepository.Received(1)
            .DeleteExceptApplicationObjectIdsAsync(Arg.Is<IEnumerable<string>>(ids => !ids.Any()));
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenMonitorNotFound_SkipsGraphFetch()
    {
        _monitorRepository.GetMonitorById(MonitorId).Returns((Monitor?)null);

        await _secretsRunner.CheckSecretsAsync();

        _azureAppSecretsFetcher.DidNotReceive()
            .FetchPasswordCredentialsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
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
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenFetcherThrows_SendsFailedNotificationIfWasHealthy()
    {
        var monitor = CreateMonitor(status: true);
        _monitorRepository.GetMonitorById(MonitorId).Returns(monitor);
        _azureAppSecretsFetcher
            .FetchPasswordCredentialsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(ThrowingAsyncEnumerable());

        await _secretsRunner.CheckSecretsAsync();

        await _monitorRepository.Received(1).UpdateMonitorStatus(MonitorId, false, 0);
        await _notificationProducer.Received(1).HandleFailedSecretsNotifications(
            monitor,
            Arg.Is<string>(m => m.Contains("Graph API error")));
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
    [InlineData(2, false)]
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
            _watchRepository,
            _monitorRepository,
            _notificationProducer,
            _monitorAlertRepository,
            _monitorHistoryRepository,
            _systemConfigurationRepository,
            Substitute.For<ILogger<SecretsRunner>>());
    }

    private void SetupRegisteredApps(params string[] objectIds)
    {
        _watchRepository.GetAllAsync().Returns(Task.FromResult(
            objectIds.Select(id => new AzureAppRegistrationWatch
            {
                Id = 1,
                ApplicationObjectId = id,
                ApplicationDisplayName = "Test App",
                AppId = "app-client-id",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            })));
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
        AppObjectId,
        "Test App",
        "app-client-id",
        keyId ?? SecretKeyId,
        "Primary secret",
        DateTimeOffset.UtcNow.AddDays(daysUntilExpiry));

    private void SetupFetcher(params AzurePasswordCredentialInfo[] credentials)
    {
        _azureAppSecretsFetcher
            .FetchPasswordCredentialsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
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
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}
