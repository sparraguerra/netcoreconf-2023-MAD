using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetCoreConf.DurableFx;

public static partial class NetCoreConfFx
{
    [FunctionName("AsyncHttpFx_Orchestrator")]
    public static async Task<List<string>> AsyncHttpFxOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var safeLog = context.CreateReplaySafeLogger(log);
        safeLog.LogError("STARTED PATTERN ASYNC HTTP");

        var outputs = new List<string>();

        for (int i = 0; i < 10; i++)
        {
            outputs.Add(await context.CallActivityAsync<string>(nameof(AsyncHttpCounter), i));
            context.SetCustomStatus($"Iteration {i} completed");
            Task.Delay(1000).Wait();
        }

        safeLog.LogError("FINISHED PATTERN ASYNC HTTP");

        return outputs;
    }

    [FunctionName(nameof(AsyncHttpCounter))]
    public static string AsyncHttpCounter([ActivityTrigger] string name, ILogger log)
    {
        log.LogWarning($"Running iteration {name}");

        return $"Executed iteration {name}!";
    }
}
