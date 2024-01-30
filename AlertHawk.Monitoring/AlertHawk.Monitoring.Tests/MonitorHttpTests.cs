using System.Net;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;

namespace AlertHawk.Monitoring.Tests;

public class MonitorHttpTests
{
    [Fact]
    public async Task should_check_http_urls()
    {
        // Arrange
        var monitor =
            new MonitorHttp
            {
                MaxRedirects = 5,
                Name = "Notification API",
                HeartBeatInterval = 60,
                Retries = 5,
                UrlToCheck = "https://dev.api.alerthawk.tech/notification/api/version",
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 10
            };

        // Act
        //var runner = new HttpClientRunner();

       // var result = await runner.CheckUrlsAsync(monitor);

        // Assert
       // Assert.True(result.ResponseStatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_check_http_with_20_urls()
    {
        // Arrange
        var items = new List<MonitorHttp>();

        for (int i = 0; i < 20; i++)
        {
            var monitor = new MonitorHttp
            {
                MaxRedirects = 5,
                Name = $"Notification API_i",
                HeartBeatInterval = 60,
                Retries = 3,
                UrlToCheck = "https://dev.api.alerthawk.tech/notification/api/version",
                CheckCertExpiry = true,
                IgnoreTlsSsl = false,
                Timeout = 10
            };
            // Act
          //  var runner = new HttpClientRunner();
          //  var result = await runner.CheckUrlsAsync(monitor);
          //  items.Add(result);
        }

        // Assert
        //foreach (var item in items)
       // {
       //     Assert.True(item.ResponseStatusCode == HttpStatusCode.OK);
       // }
    }
}