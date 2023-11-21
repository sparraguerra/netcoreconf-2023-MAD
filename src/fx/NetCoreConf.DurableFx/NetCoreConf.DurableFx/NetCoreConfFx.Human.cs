using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace NetCoreConf.DurableFx;

public static partial class NetCoreConfFxHuman
{

    [FunctionName("HumanFX_Orchestrator")]
    public static async Task<string> HumanFXOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var safeLog = context.CreateReplaySafeLogger(log);
        safeLog.LogError("STARTED PATTERN HUMAN INTERACTION");

        await context.CallActivityAsync("HumanFxRequestApproval", context.InstanceId);
        using (var timeoutCts = new CancellationTokenSource())
        {
            DateTime dueTime = context.CurrentUtcDateTime.AddHours(72);
            Task durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);

            Task<bool> approvalEvent = context.WaitForExternalEvent<bool>("ApprovalEvent");
            if (approvalEvent == await Task.WhenAny(approvalEvent, durableTimeout))
            {
                timeoutCts.Cancel();
                await context.CallActivityAsync("ProcessApproval", approvalEvent.Result);
            }
            else
            {
                await context.CallActivityAsync("Escalate", null);
            }
        }

        safeLog.LogError("FINISHED PATTERN HUMAN INTERACTION");

        return "Completed";
    }

    [FunctionName(nameof(HumanFxRequestApproval))]
    public static void HumanFxRequestApproval([ActivityTrigger] string instanceId, ILogger log)
    {
        log.LogWarning($"Requesting approval for {instanceId}");
    }

    [FunctionName("RaiseEventToOrchestration")]
    public static async Task Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] string instanceId,
        [DurableClient] IDurableOrchestrationClient client)
    {
        bool isApproved = true;
        await client.RaiseEventAsync(instanceId, "ApprovalEvent", isApproved);
    }

    [FunctionName(nameof(ProcessApproval))]
    public static void ProcessApproval([ActivityTrigger] bool isApproved, ILogger log)
    {
        log.LogWarning($"ProcessApproval {isApproved}");
    }

    [FunctionName(nameof(Escalate))]
    public static void Escalate([ActivityTrigger] string instanceId, ILogger log)
    {
        log.LogWarning($"Escalate {instanceId}");
    }
}
