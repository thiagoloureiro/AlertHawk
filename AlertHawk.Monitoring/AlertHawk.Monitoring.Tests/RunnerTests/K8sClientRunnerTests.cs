using System.Text;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using k8s.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AlertHawk.Monitoring.Tests.RunnerTests
{
    public class K8sClientRunnerTests
    {
        private readonly IK8sNodeClient _k8sNodeClient;
        private readonly K8sClientRunner _runner;

        public K8sClientRunnerTests()
        {
            _k8sNodeClient = Substitute.For<IK8sNodeClient>();
            _runner = new K8sClientRunner(
                Substitute.For<ILogger<K8sClientRunner>>(),
                Substitute.For<IMonitorHistoryRepository>(),
                Substitute.For<IMonitorRepository>(),
                Substitute.For<INotificationProducer>(),
                Substitute.For<IMonitorAlertRepository>(),
                Substitute.For<ISystemConfigurationRepository>(),
                _k8sNodeClient);
        }

        [Fact]
        public async Task RunMonitorAsync_WhenCalled_ReturnsTrue()
        {
            _k8sNodeClient.ListNodesAsync(Arg.Any<string>()).Returns(new List<V1Node> { Node("worker-1", ready: true) });

            var result = await _runner.CallK8S(Monitor());

            Assert.True(result.succeeded);
            Assert.NotNull(result.responseMessage);
            Assert.NotNull(result.monitorHistory);
            Assert.True(result.monitorHistory.Status);
            Assert.Equal(1, result.monitorHistory.MonitorId);
            await _k8sNodeClient.Received(1).ListNodesAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task CallK8S_WhenNodeIsNotReady_ReturnsFailure()
        {
            _k8sNodeClient.ListNodesAsync(Arg.Any<string>()).Returns(new List<V1Node> { Node("worker-1", ready: false) });

            var result = await _runner.CallK8S(Monitor());

            Assert.False(result.succeeded);
            Assert.Contains("worker-1 is not ready", result.responseMessage);
            Assert.False(result.monitorHistory.Status);
        }

        private static MonitorK8s Monitor()
        {
            return new MonitorK8s
            {
                MonitorId = 1,
                ClusterName = "TestCluster",
                KubeConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes("kubeconfig")),
                LastStatus = false
            };
        }

        private static V1Node Node(string name, bool ready)
        {
            return new V1Node
            {
                Metadata = new V1ObjectMeta { Name = name },
                Status = new V1NodeStatus
                {
                    Conditions =
                    [
                        new V1NodeCondition { Type = "Ready", Status = ready ? "True" : "False" },
                        new V1NodeCondition { Type = "MemoryPressure", Status = "False" },
                        new V1NodeCondition { Type = "DiskPressure", Status = "False" },
                        new V1NodeCondition { Type = "PIDPressure", Status = "False" }
                    ]
                }
            };
        }
    }
}
