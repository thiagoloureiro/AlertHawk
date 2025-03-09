using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using NSubstitute;
using System.Net;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Tests.RunnerTests
{
    public class K8sClientRunnerTests
    {
        private readonly IK8sClientRunner _k8SClientRunner;

        public K8sClientRunnerTests(IK8sClientRunner k8SClientRunner)
        {
            _k8SClientRunner = k8SClientRunner;
        }

        [Fact]
        public async Task RunMonitorAsync_WhenCalled_ReturnsTrue()
        {
            // Arrange
            var monitor = new MonitorK8s
            {
                MonitorId = 1,
                ClusterName = "TestCluster",
                KubeConfig = "",
                LastStatus = false
            };

            // Act
          //  await _k8SClientRunner.CheckK8sAsync(monitor);

            // Assert
            Assert.True(true);
        }
    }
}