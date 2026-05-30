using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.Producers;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.Producers;

public class NotificationProducerSecretsTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMonitorNotificationRepository _monitorNotificationRepository;
    private readonly NotificationProducer _notificationProducer;

    public NotificationProducerSecretsTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _monitorNotificationRepository = Substitute.For<IMonitorNotificationRepository>();
        _notificationProducer = new NotificationProducer(
            _publishEndpoint,
            _monitorNotificationRepository,
            Substitute.For<ILogger<NotificationProducer>>());
    }

    [Fact]
    public async Task HandleFailedSecretsNotifications_PublishesForEachNotification()
    {
        var monitor = new Monitor
        {
            Id = 1,
            Name = "Azure Secrets",
            MonitorRegion = MonitorRegion.NorthAmerica,
            MonitorEnvironment = MonitorEnvironment.Production
        };

        _monitorNotificationRepository.GetMonitorNotifications(1).Returns(new[]
        {
            new MonitorNotification { MonitorId = 1, NotificationId = 10 },
            new MonitorNotification { MonitorId = 1, NotificationId = 20 }
        });

        await _notificationProducer.HandleFailedSecretsNotifications(monitor, "Secret expiring soon");

        await _monitorNotificationRepository.Received(1).GetMonitorNotifications(1);
        Assert.Equal(2, _publishEndpoint.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "Publish"));
    }

    [Fact]
    public async Task HandleSuccessSecretsNotifications_PublishesForEachNotification()
    {
        var monitor = new Monitor
        {
            Id = 1,
            Name = "Azure Secrets",
            MonitorRegion = MonitorRegion.NorthAmerica,
            MonitorEnvironment = MonitorEnvironment.Production
        };

        _monitorNotificationRepository.GetMonitorNotifications(1).Returns(new[]
        {
            new MonitorNotification { MonitorId = 1, NotificationId = 10 }
        });

        await _notificationProducer.HandleSuccessSecretsNotifications(monitor, "All secrets healthy");

        await _monitorNotificationRepository.Received(1).GetMonitorNotifications(1);
        Assert.Single(_publishEndpoint.ReceivedCalls().Where(c => c.GetMethodInfo().Name == "Publish"));
    }
}
