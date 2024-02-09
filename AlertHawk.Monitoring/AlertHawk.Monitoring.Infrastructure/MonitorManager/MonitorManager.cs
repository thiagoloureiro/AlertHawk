using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Infrastructure.Utils;
using EasyMemoryCache;
using Hangfire;
using Hangfire.Storage;

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

            // HTTP
            var lstMonitorByHttpType = monitorListByIds.Where(x => x.MonitorTypeId == 1);
            var monitorByHttpType = lstMonitorByHttpType.ToList();
            if (monitorByHttpType.Any())
            {
                var httpMonitorIds = monitorByHttpType.Select(x => x.Id).ToList();
                var lstMonitors = await _monitorRepository.GetHttpMonitorByIds(httpMonitorIds);

                GlobalVariables.TaskList = httpMonitorIds;

                var lstStringsToAdd = new List<string>();
                var monitorHttps = lstMonitors.ToList();

                foreach (var monitorHttp in monitorHttps)
                {
                    monitorHttp.LastStatus =
                        monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Status;
                    monitorHttp.Name = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Name;
                    monitorHttp.Retries = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Retries;

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
    }

    public async Task StartMonitorHeartBeatManager()
    {
        try
        {
            var agentLocationEnabled = GetEnableLocationApi();
            MonitorAgent monitorAgent;

            if (agentLocationEnabled)
            {
                var locationData = _caching.GetOrSetObjectFromCache("locationData", 60, IPAddressUtils.GetLocation);

                monitorAgent = new MonitorAgent
                {
                    Hostname = Environment.MachineName,
                    TimeStamp = DateTime.UtcNow,
                    Location = new LocationDetails
                    {
                        Country = locationData.Country,
                        Continent = locationData.Continent
                    }
                };
            }
            else
            {
                monitorAgent = new MonitorAgent
                {
                    Hostname = Environment.MachineName,
                    TimeStamp = DateTime.UtcNow
                };
            }

            await _monitorAgentRepository.ManageMonitorStatus(monitorAgent);
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }
    
    static bool GetEnableLocationApi()
    {
        string enableLocationApiValue = Environment.GetEnvironmentVariable("enable_location_api");
        if (!string.IsNullOrEmpty(enableLocationApiValue) && bool.TryParse(enableLocationApiValue, out bool result))
        {
            return result;
        }
        // Default value if environment variable is not set or not a valid boolean
        return false;
    }

    public async Task StartMasterMonitorAgentTaskManager()
    {
        try
        {
            // Only MasterNode is responsible for Managing Tasks
            if (GlobalVariables.MasterNode)
            {
                var lstMonitorAgentTasks = new List<MonitorAgentTasks>();
                var iEnumerable = await _monitorRepository.GetMonitorList();
                var monitorList = iEnumerable.ToList();
                var monitorAgents = await _monitorAgentRepository.GetAllMonitorAgents();

                var countMonitor = monitorList.Count();
                var countAgents = monitorAgents.Count();

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

                if (GlobalVariables.TaskList != null)
                {
                    GlobalVariables.TaskList.Clear();
                }

                GlobalVariables.TaskList = new List<int>();

                foreach (var item in lstMonitorAgentTasks)
                {
                    if (item.MonitorAgentId == GlobalVariables.NodeId)
                    {
                        GlobalVariables.TaskList.Add(item.MonitorId);
                    }
                }

                await _monitorAgentRepository.UpsertMonitorAgentTasks(lstMonitorAgentTasks);
            }
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }
}