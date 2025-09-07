using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;

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
            var configfile = File.ReadAllText("/home/vsts/work/_temp/local.yaml");

            var base64Config = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(configfile));

            var monitor = new MonitorK8s
            {
                MonitorId = 1,
                ClusterName = "TestCluster",
                KubeConfig = base64Config,
                LastStatus = false
            };

            // Act
            var result = await _k8SClientRunner.CallK8S(monitor);

            // Assert
            Assert.True(result.succeeded);
            Assert.NotNull(result.responseMessage);
            Assert.NotNull(result.monitorHistory);
        }
    }
}