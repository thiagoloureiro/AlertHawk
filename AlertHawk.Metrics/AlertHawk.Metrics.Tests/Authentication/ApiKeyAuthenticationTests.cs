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
    public void Match_ConfiguredKey_ReturnsTheApiKeyIdentity()
    {
        var validator = Validator("myapikey");

        Assert.True(validator.IsConfigured);
        Assert.Equal(ApiKeyValidator.IdentityName, validator.Match("myapikey"));
    }

    [Fact]
    public void Match_WrongKey_ReturnsNull()
    {
        var validator = Validator("myapikey");

        Assert.Null(validator.Match("other-key"));
        Assert.Null(validator.Match("myapikey-extra"));
        Assert.Null(validator.Match(""));
    }

    [Fact]
    public void Match_BlankKey_IsNotConfigured()
    {
        var validator = Validator("   ");

        Assert.False(validator.IsConfigured);
        Assert.Null(validator.Match("myapikey"));
    }

    [Fact]
    public void Match_NoKeyConfigured_ReturnsNull()
    {
        var validator = new ApiKeyValidator(new ConfigurationBuilder().Build());

        Assert.False(validator.IsConfigured);
        Assert.Null(validator.Match("myapikey"));
    }

    private static ApiKeyValidator Validator(string key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ApiKeyValidator.ConfigurationKey] = key,
            })
            .Build();

        return new ApiKeyValidator(configuration);
    }
}

public class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task Authenticate_ValidKey_SucceedsWithKeyName()
    {
        var context = Context("myapikey");
        context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = "myapikey";

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

        Assert.True(result.Succeeded);
        Assert.Equal(ApiKeyValidator.IdentityName, result.Principal?.Identity?.Name);
    }

    [Fact]
    public async Task Authenticate_MissingHeader_DoesNotAuthenticate()
    {
        var context = Context("myapikey");

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task Authenticate_InvalidKey_Fails()
    {
        var context = Context("myapikey");
        context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = "wrong-key";

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    private static DefaultHttpContext Context(string key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ApiKeyValidator.ConfigurationKey] = key,
            })
            .Build();
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
