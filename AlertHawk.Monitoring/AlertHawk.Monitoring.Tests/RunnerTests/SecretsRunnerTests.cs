using AlertHawk.Monitoring.Domain.Configuration;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AlertHawk.Monitoring.Tests.RunnerTests;

public class SecretsRunnerTests
{
    private readonly IAzureAppSecretsFetcher _azureAppSecretsFetcher;
    private readonly IAzureAppSecretRepository _azureAppSecretRepository;
    private readonly IAzureAppRegistrationWatchRepository _watchRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly SecretsRunner _secretsRunner;

    private const string AppObjectId = "app-obj-id";
    private static readonly Guid SecretKeyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SecretsRunnerTests()
    {
        _azureAppSecretsFetcher = Substitute.For<IAzureAppSecretsFetcher>();
        _azureAppSecretRepository = Substitute.For<IAzureAppSecretRepository>();
        _watchRepository = Substitute.For<IAzureAppRegistrationWatchRepository>();
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
            DaysBeforeExpiryToAlert = 30
        });
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenDisabled_DoesNotCallGraph()
    {
        var runner = CreateRunner(new AzureSecretsOptions { Enabled = false });

        await runner.CheckSecretsAsync();

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
    public async Task CheckSecretsAsync_UpsertsEachFetchedSecret()
    {
        SetupFetcher(
            HealthyCredential(daysUntilExpiry: 90),
            HealthyCredential(daysUntilExpiry: 60, keyId: Guid.Parse("22222222-2222-2222-2222-222222222222")));

        await _secretsRunner.CheckSecretsAsync();

        await _azureAppSecretRepository.Received(2).UpsertAsync(Arg.Any<AzureAppSecret>());
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenExpiringSecret_SetsIsExpiring()
    {
        SetupFetcher(HealthyCredential(daysUntilExpiry: 10));

        await _secretsRunner.CheckSecretsAsync();

        await _azureAppSecretRepository.Received(1).UpsertAsync(Arg.Is<AzureAppSecret>(s => s.IsExpiring));
    }

    [Fact]
    public async Task CheckSecretsAsync_WhenFetcherThrows_DoesNotThrow()
    {
        _azureAppSecretsFetcher
            .FetchPasswordCredentialsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(ThrowingAsyncEnumerable());

        await _secretsRunner.CheckSecretsAsync();

        await _azureAppSecretRepository.DidNotReceive().UpsertAsync(Arg.Any<AzureAppSecret>());
    }

    private SecretsRunner CreateRunner(AzureSecretsOptions options)
    {
        _settingsProvider.GetSettingsAsync().Returns(Task.FromResult(options));

        return new SecretsRunner(
            _settingsProvider,
            _azureAppSecretsFetcher,
            _azureAppSecretRepository,
            _watchRepository,
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
