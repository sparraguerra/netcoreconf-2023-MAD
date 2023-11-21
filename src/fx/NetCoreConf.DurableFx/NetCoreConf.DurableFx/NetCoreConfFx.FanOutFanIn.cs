using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;

namespace NetCoreConf.DurableFx;

public static partial class NetCoreConfFx
{
    [FunctionName("FanOutFanInFx_Orchestrator")]
    public static async Task FanOutFanInFxOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
    {
        var safeLog = context.CreateReplaySafeLogger(log);
        safeLog.LogError("STARTED PATTERN FAN OUT FAN IN");

        var parallelTasks = new List<Task<int>>();

        string[] workBatch = await context.CallActivityAsync<string[]>(nameof(F1), null);
        for (int i = 0; i < workBatch.Length; i++)
        {
            Task<int> task = context.CallActivityAsync<int>(nameof(F2), workBatch[i]);
            parallelTasks.Add(task);
        }

        await Task.WhenAll(parallelTasks);

        int sum = parallelTasks.Sum(t => t.Result);
        await context.CallActivityAsync(nameof(F3), sum);
        safeLog.LogError("FINISHED PATTERN FAN OUT FAN IN");
    }

    [FunctionName(nameof(F1))]
    public static string[] F1([ActivityTrigger] string name, ILogger log)
    {
        log.LogWarning(" * STARTING WITH F1", name);
        var result = new string[]
        {
            "Process 1",
            "Process 2",
            "Process 3",
            "Process 4",
            "Process 5",
            "Process 6",
            "Process 7",
            "Process 8",
            "Process 9",
            "Process 10"
        };

        return result;
    }

    [FunctionName(nameof(F2))]
    public static void F2([ActivityTrigger] string name, ILogger log)
    {
        log.LogWarning($"  ** Processing F2 with parameter {name}.");
    }

    [FunctionName(nameof(F3))]
    public static void F3([ActivityTrigger] string name, ILogger log)
    {
        log.LogWarning(" * FINISHING WITH F3", name);
    }
}
