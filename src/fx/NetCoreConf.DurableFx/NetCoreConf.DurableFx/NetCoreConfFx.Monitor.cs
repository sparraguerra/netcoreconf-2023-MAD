using Microsoft.Azure.WebJobs;
using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;
using System.Threading;
using System.Security.Cryptography;

namespace NetCoreConf.DurableFx;

public static partial class NetCoreConfFx
{

    [FunctionName("MonitorFX_Orchestrator")]
    public static async Task<string> HumanFXOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var safeLog = context.CreateReplaySafeLogger(log);
        safeLog.LogError("STARTED PATTERN MONITOR");

        int jobId = context.GetInput<int>(); 
        int pollingInterval = GetPollingInterval();
        DateTime expiryTime = GetExpiryTime();

        while (context.CurrentUtcDateTime < expiryTime)
        {
            var jobStatus = await context.CallActivityAsync<string>("GetJobStatus", jobId);
            if (jobStatus == "Completed")
            {
                await context.CallActivityAsync("SendAlert", jobId);
                break;
            }

            var nextCheck = context.CurrentUtcDateTime.AddSeconds(pollingInterval);
            await context.CreateTimer(nextCheck, CancellationToken.None);
        }

        safeLog.LogError("FINISHED PATTERN MONITOR");

        return "Completed";
    }

    private static int GetPollingInterval()
    {
        return 5;
    }

    private static DateTime GetExpiryTime()
    {
        return DateTime.UtcNow.AddMinutes(1);
    }

    [FunctionName(nameof(GetJobStatus))]
    public static async Task<string> GetJobStatus(
        [ActivityTrigger] string jobId,
        ILogger log)
    {
        var status = new Random().Next(0, 10);

        return status > 5 ? "Completed" : "Running";  
    }

    [FunctionName(nameof(SendAlert))]
    public static void SendAlert(
        [ActivityTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        log.LogWarning($"Alerting job with {context.InstanceId} is completed.");
    }
}
