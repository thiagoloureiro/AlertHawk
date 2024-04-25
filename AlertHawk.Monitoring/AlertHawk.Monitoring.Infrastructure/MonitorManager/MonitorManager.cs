using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Utils;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using AlertHawk.Monitoring.Infrastructure.Utils;
using EasyMemoryCache;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using System.Net.Http;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.MonitorManager;

public class MonitorManager : IMonitorManager, IJob
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;
    private readonly IMonitorRepository _monitorRepository;
    private readonly ICaching _caching;
    private readonly ISchedulerFactory _schedulerFactory;
    private IScheduler _scheduler;
    private readonly IHttpClientScreenshot _httpClientScreenshot;
    private readonly INotificationProducer _notificationProducer;
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitorManager(IMonitorAgentRepository monitorAgentRepository, IMonitorRepository monitorRepository,
        ICaching caching, IHttpClientScreenshot httpClientScreenshot, INotificationProducer notificationProducer, IHttpClientFactory httpClientFactory)
    {
        _monitorAgentRepository = monitorAgentRepository;
        _monitorRepository = monitorRepository;
        _caching = caching;
        _httpClientScreenshot = httpClientScreenshot;
        _notificationProducer = notificationProducer;
        _httpClientFactory = httpClientFactory;
        _schedulerFactory = new StdSchedulerFactory();
    }

    public async Task StartRunnerManager()
    {
        try
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
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            throw;
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
                monitorHttp.MonitorId = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId).Id;

                JsonUtils.ConvertJsonToTuple(monitorHttp);

                string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                lstStringsToAdd.Add(jobId);

                _scheduler = await _schedulerFactory.GetScheduler();

                // Define the job data
                JobDataMap jobDataMap = new JobDataMap();
                jobDataMap["monitorHttp"] = monitorHttp;
                jobDataMap.Put("MonitorRepository", _monitorRepository);
                jobDataMap.Put("HttpClientScreenshot", _httpClientScreenshot);
                jobDataMap.Put("NotificationProducer", _notificationProducer);
                jobDataMap.Put("HttpClientFactory", _httpClientFactory);

                // Start the scheduler
                await _scheduler.Start();

                // Define the job and tie it to our class
                IJobDetail job = JobBuilder.Create<HttpClientRunner>()
                    .WithIdentity(jobId, "group1")
                    .UsingJobData(jobDataMap)
                    .Build();

                // Define a trigger that will fire the job
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"{jobId}_httpClientTrigger", "group1")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(30)  // Interval at which the job will run (e.g., every 30 seconds)
                        .RepeatForever())
                    .Build();

                // Tell Quartz to schedule the job using our trigger
                await _scheduler.ScheduleJob(job, trigger);
            }

            //            var myJobScheduler = new MyJobScheduler(_scheduler);
            //  await myJobScheduler.ScheduleJob("jobId1", 60);

            // IEnumerable<RecurringJobDto> recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            //   recurringJobs = recurringJobs.Where(x => x.Id.Contains("StartRunnerManager_CheckUrlsAsync_JobId"))
            //    .ToList();

            //   foreach (var job in recurringJobs)
            //  {
            //      if (!lstStringsToAdd.Contains(job.Id))
            //      {
            //          RecurringJob.RemoveIfExists(job.Id);
            //      }
            //  }

            foreach (var monitorHttp in monitorHttps)
            {
                string jobId = $"StartRunnerManager_CheckUrlsAsync_JobId_{monitorHttp.MonitorId}";
                Thread.Sleep(50);
                var monitor = monitorByHttpType.FirstOrDefault(x => x.Id == monitorHttp.MonitorId);

                //  RecurringJob.AddOrUpdate<IHttpClientRunner>(jobId, queue: Environment.MachineName.ToLower(), x => x.CheckUrlsAsync(monitorHttp),
                //      $"*/{monitor?.HeartBeatInterval} * * * *");
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
            recurringJobs = recurringJobs.Where(x => x.Id.Contains("StartRunnerManager_CheckTcpAsync_JobId"))
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
                RecurringJob.AddOrUpdate<ITcpClientRunner>(jobId, queue: Environment.MachineName.ToLower(), x => x.CheckTcpAsync(monitorTcp),
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
                    MonitorRegion = MonitorUtils.GetMonitorRegionVariable()
                };
            }

            await _monitorAgentRepository.ManageMonitorStatus(monitorAgent);
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            throw;
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
            throw;
        }
    }

    private async Task SetAgentTasksPerMonitorPerRegion(int monitorRegion)
    {
        var lstMonitorAgentTasks = new List<MonitorAgentTasks>();
        var monitors = await _monitorRepository.GetMonitorRunningList();
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

    public async Task Execute(IJobExecutionContext context)
    {
        var jobKey = context.JobDetail.Key.Name;
        switch (jobKey)
        {
            case "StartMonitorHeartBeatManager":
                await StartMonitorHeartBeatManager();
                break;

            case "StartMasterMonitorAgentTaskManager":
                await StartMasterMonitorAgentTaskManager();
                break;

            case "StartRunnerManager":
                await StartRunnerManager();
                break;

            default:
                throw new NotSupportedException($"Unsupported job key: {jobKey}");
        }
    }
}