using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Utils;
using AlertHawk.Monitoring.Infrastructure.Utils;
using EasyMemoryCache;
using Hangfire;
using Hangfire.Storage;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.MonitorManager;

public class MonitorManager : IMonitorManager
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;
    private readonly IMonitorRepository _monitorRepository;
    private readonly ICaching _caching;

    public MonitorManager(IMonitorAgentRepository monitorAgentRepository, IMonitorRepository monitorRepository,
        ICaching caching)
    {
        _monitorAgentRepository = monitorAgentRepository;
        _monitorRepository = monitorRepository;
        _caching = caching;
    }

    public async Task StartRunnerManager()
    {
        var tasksToMonitor = await _monitorAgentRepository.GetAllMonitorAgentTasksByAgentId(GlobalVariables.NodeId);
        if (tasksToMonitor.Any())
        {
            var monitorIds = tasksToMonitor.Select(x => x.MonitorId).ToList();
            var monitorListByIds = await _monitorRepository.GetMonitorListByIds(monitorIds);

            await StartHttpMonitorJobs(monitorListByIds);
            await StartTcpMonitorJobs(monitorListByIds);
        }
    }

    private async Task StartHttpMonitorJobs(IEnumerable<Monitor> monitorListByIds)
    {
        var lstMonitorByHttpType = monitorListByIds.Where(x => x.MonitorTypeId == 1);
        var monitorByHttpType = lstMonitorByHttpType.ToList();
        if (monitorByHttpType.Any())
        {
            var httpMonitorIds = monitorByHttpType.Select(x => x.Id).ToList();
            var lstMonitors = await _monitorRepository.GetHttpMonitorByIds(httpMonitorIds);


            GlobalVariables.HttpTaskList = httpMonitorIds;

            var lstStringsToAdd = new List<string>();
            var monitorHttps = lstMonitors.ToList();

            foreach (var monitorHttp in monitorHttps)
            {
                monitorHttp.LastStatus =
                    monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Status;
                monitorHttp.Name = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Name;
                monitorHttp.Retries = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Retries;

                JsonUtils.ConvertJsonToTuple(monitorHttp);

                string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                lstStringsToAdd.Add(jobId);
            }

            IEnumerable<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            recurringJobs = recurringJobs.Where(x => x.Id.StartsWith("StartRunnerManager_CheckUrlsAsync_JobId"))
                .ToList();

            foreach (var job in recurringJobs)
            {
                if (!lstStringsToAdd.Contains(job.Id))
                {
                    RecurringJob.RemoveIfExists(job.Id);
                }
            }

            foreach (var monitorHttp in monitorHttps)
            {
                string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                Thread.Sleep(50);
                var monitor = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId);

                RecurringJob.AddOrUpdate<IHttpClientRunner>(jobId, x => x.CheckUrlsAsync(monitorHttp),
                    $"*/{monitor?.HeartBeatInterval} * * * *");
            }
        }
    }

    private async Task StartTcpMonitorJobs(IEnumerable<Monitor> monitorListByIds)
    {
        var lstMonitorByTcpType = monitorListByIds.Where(x => x.MonitorTypeId == 3);
        var monitorByTcpType = lstMonitorByTcpType.ToList();
        if (monitorByTcpType.Any())
        {
            var tcpMonitorIds = monitorByTcpType.Select(x => x.Id).ToList();
            var lstMonitors = await _monitorRepository.GetTcpMonitorByIds(tcpMonitorIds);

            GlobalVariables.TcpTaskList = tcpMonitorIds;

            var lstStringsToAdd = new List<string>();
            var monitorTcpList = lstMonitors.ToList();

            foreach (var monitorTcp in monitorTcpList)
            {
                monitorTcp.LastStatus =
                    monitorByTcpType.FirstOrDefault(x => x.Id == monitorTcp.MonitorId).Status;
                monitorTcp.Name = monitorByTcpType.FirstOrDefault(x => x.Id == monitorTcp.MonitorId).Name;
                monitorTcp.Retries = monitorByTcpType.FirstOrDefault(x => x.Id == monitorTcp.MonitorId).Retries;

                string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorTcp.MonitorId}";
                lstStringsToAdd.Add(jobId);
            }

            IEnumerable<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            recurringJobs = recurringJobs.Where(x => x.Id.StartsWith("StartRunnerManager_CheckTcpAsync_JobId"))
                .ToList();

            foreach (var job in recurringJobs)
            {
                if (!lstStringsToAdd.Contains(job.Id))
                {
                    RecurringJob.RemoveIfExists(job.Id);
                }
            }

            foreach (var monitorTcp in monitorTcpList)
            {
                string jobId = $"StartRunnerManager_CheckTcpAsync_JobId_{monitorTcp.MonitorId}";
                Thread.Sleep(50);
                var monitor = monitorByTcpType.FirstOrDefault(x => x.Id == monitorTcp.MonitorId);
                RecurringJob.AddOrUpdate<ITcpClientRunner>(jobId, x => x.CheckTcpAsync(monitorTcp),
                    $"*/{monitor?.HeartBeatInterval} * * * *");
            }
        }
    }

    public async Task StartMonitorHeartBeatManager()
    {
        try
        {
            var agentLocationEnabled = VariableUtils.GetBoolEnvVariable("enable_location_api");
            MonitorAgent monitorAgent;

            if (agentLocationEnabled)
            {
                var region =
                    await _caching.GetOrSetObjectFromCacheAsync("locationDataKey", 600, IPAddressUtils.GetLocation);

                monitorAgent = new MonitorAgent
                {
                    Hostname = Environment.MachineName,
                    TimeStamp = DateTime.UtcNow,
                    MonitorRegion = region
                };
            }
            else
            {
                monitorAgent = new MonitorAgent
                {
                    Hostname = Environment.MachineName,
                    TimeStamp = DateTime.UtcNow,
                    MonitorRegion = GetMonitorRegionVariable()
                };
            }

            await _monitorAgentRepository.ManageMonitorStatus(monitorAgent);
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }

    public async Task StartMasterMonitorAgentTaskManager()
    {
        try
        {
            // Only MasterNode is responsible for Managing Tasks
            if (GlobalVariables.MasterNode)
            {
                foreach (var day in Enum.GetValues(typeof(MonitorRegion)))
                {
                    await SetAgentTasksPerMonitorPerRegion((int)day);
                }
            }
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }

    private async Task SetAgentTasksPerMonitorPerRegion(int monitorRegion)
    {
        var lstMonitorAgentTasks = new List<MonitorAgentTasks>();
        var monitors = await _monitorRepository.GetMonitorList();
        monitors = monitors.Where(x => (int)x.MonitorRegion == monitorRegion);

        var monitorAgents = await _monitorAgentRepository.GetAllMonitorAgents();
        monitorAgents = monitorAgents.Where(x => (int)x.MonitorRegion == monitorRegion).ToList();

        var monitorList = monitors.Where(x => x?.Paused == false).ToList();

        var countMonitor = monitorList.Count;
        var countAgents = monitorAgents.Count;

        if (countAgents > 0 && countMonitor > 0)
        {
            int tasksPerMonitor = countMonitor / countAgents;
            int extraTasks = countMonitor % countAgents;

            int currentIndex = 0;
            int indexAgent = 0;

            for (int i = 0; i < countMonitor; i++)
            {
                int tasksToTake = tasksPerMonitor + (i < extraTasks ? 1 : 0);

                var tasksForMonitor = monitorList.Skip(currentIndex).Take(tasksToTake).ToList();

                if (!tasksForMonitor.Any())
                {
                    break;
                }

                var agent = monitorAgents.ElementAt(indexAgent);

                foreach (var task in tasksForMonitor)
                {
                    lstMonitorAgentTasks.Add(new MonitorAgentTasks
                    {
                        MonitorId = task.Id,
                        MonitorAgentId = agent.Id
                    });
                }

                indexAgent += 1;
                currentIndex += tasksToTake;
            }

            await _monitorAgentRepository.UpsertMonitorAgentTasks(lstMonitorAgentTasks, monitorRegion);
        }
    }

    static MonitorRegion GetMonitorRegionVariable()
    {
        string? monitorRegion = Environment.GetEnvironmentVariable("monitor_region");
        if (!string.IsNullOrEmpty(monitorRegion) && int.TryParse(monitorRegion, out int result))
        {
            MonitorRegion value = (MonitorRegion)result;
            return value;
        }

        // Default value if environment variable is not set or not a valid boolean
        return MonitorRegion.Custom;
    }
}