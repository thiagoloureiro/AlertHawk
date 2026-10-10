using System.Reflection;
using AlertHawk.Metrics.API.Authentication;
using AlertHawk.Metrics.API.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlertHawk.Metrics.Tests.Authentication;

public class ApiKeyValidatorTests
{
    [Fact]
    public void Match_ConfiguredKey_ReturnsItsName()
    {
        var validator = Validator(
            ("rancher-extension", "metrics-key-0123456789"),
            ("other", "another-key-0123456789"));

        Assert.Equal("other", validator.Match("another-key-0123456789"));
        Assert.Equal(2, validator.Count);
    }

    [Fact]
    public void Match_WrongKey_ReturnsNull()
    {
        var validator = Validator(("rancher-extension", "metrics-key-0123456789"));

        Assert.Null(validator.Match("metrics-key-0123456780"));
        Assert.Null(validator.Match("metrics-key-0123456789-extra"));
        Assert.Null(validator.Match(""));
    }

    [Fact]
    public void Match_NoKeysConfigured_ReturnsNull()
    {
        var validator = new ApiKeyValidator(new ConfigurationBuilder().Build());

        Assert.Equal(0, validator.Count);
        Assert.Null(validator.Match("metrics-key-0123456789"));
    }

    [Fact]
    public void Match_ApiKeysEnvironmentList_AcceptsEachKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API_KEYS"] = " first-key-0123456789 , second-key-0123456789 ",
            })
            .Build();
        var validator = new ApiKeyValidator(configuration);

        Assert.Equal("api-key-1", validator.Match("first-key-0123456789"));
        Assert.Equal("api-key-2", validator.Match("second-key-0123456789"));
    }

    [Fact]
    public void Match_BlankAndDuplicateKeys_AreIgnored()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKeys:Keys:0:Name"] = "primary",
                ["ApiKeys:Keys:0:Key"] = "metrics-key-0123456789",
                ["ApiKeys:Keys:1:Name"] = "blank",
                ["ApiKeys:Keys:1:Key"] = "   ",
                ["ApiKeys:Keys:2:Name"] = "duplicate",
                ["ApiKeys:Keys:2:Key"] = "metrics-key-0123456789",
                ["API_KEYS"] = "metrics-key-0123456789",
            })
            .Build();
        var validator = new ApiKeyValidator(configuration);

        Assert.Equal(1, validator.Count);
        Assert.Equal("primary", validator.Match("metrics-key-0123456789"));
    }

    private static ApiKeyValidator Validator(params (string Name, string Key)[] keys)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < keys.Length; i++)
        {
            values[$"ApiKeys:Keys:{i}:Name"] = keys[i].Name;
            values[$"ApiKeys:Keys:{i}:Key"] = keys[i].Key;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        return new ApiKeyValidator(configuration);
    }
}

public class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task Authenticate_ValidKey_SucceedsWithKeyName()
    {
        var context = Context(("rancher-extension", "metrics-key-0123456789"));
        context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = "metrics-key-0123456789";

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

        Assert.True(result.Succeeded);
        Assert.Equal("rancher-extension", result.Principal?.Identity?.Name);
    }

    [Fact]
    public async Task Authenticate_MissingHeader_DoesNotAuthenticate()
    {
        var context = Context(("rancher-extension", "metrics-key-0123456789"));

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task Authenticate_InvalidKey_Fails()
    {
        var context = Context(("rancher-extension", "metrics-key-0123456789"));
        context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = "wrong-key-0123456789";

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    private static DefaultHttpContext Context(params (string Name, string Key)[] keys)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < keys.Length; i++)
        {
            values[$"ApiKeys:Keys:{i}:Name"] = keys[i].Name;
            values[$"ApiKeys:Keys:{i}:Key"] = keys[i].Key;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IApiKeyValidator>(new ApiKeyValidator(configuration));
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName,
                _ => { });

        var provider = services.BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = provider };
    }
}

public class MetricsEndpointAuthTests
{
    [Theory]
    [InlineData(nameof(MetricsController.GetMetricsByNamespace))]
    [InlineData(nameof(MetricsController.GetMetricsByNamespaceName))]
    [InlineData(nameof(MetricsController.GetNodeMetrics))]
    [InlineData(nameof(MetricsController.GetNodeMetricsByName))]
    [InlineData(nameof(MetricsController.GetPvcMetrics))]
    [InlineData(nameof(MetricsController.GetPvcMetricsByNamespace))]
    [InlineData(nameof(MetricsController.GetUniqueClusterNames))]
    [InlineData(nameof(MetricsController.GetUniqueNamespaceNames))]
    public void ReadEndpoints_AcceptApiKeyPolicy(string methodName)
    {
        var authorize = Authorize(methodName);

        Assert.Equal(MetricsPolicies.Read, authorize.Policy);
    }

    [Theory]
    [InlineData(nameof(MetricsController.CleanupMetrics))]
    [InlineData(nameof(MetricsController.GetPodLogs))]
    [InlineData(nameof(MetricsController.GetPodLogsByPod))]
    public void OtherEndpoints_DoNotUseTheReadPolicy(string methodName)
    {
        var authorize = Authorize(methodName);

        Assert.True(string.IsNullOrEmpty(authorize.Policy));
    }

    private static AuthorizeAttribute Authorize(string methodName)
    {
        var method = typeof(MetricsController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        return Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
    }
}
