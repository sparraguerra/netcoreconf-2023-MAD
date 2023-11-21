using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetCoreConf.DurableFx;

public static partial class NetCoreConfFx
{
    [FunctionName("ChainFx_Orchestrator")]
    public static async Task<List<string>> ChainFxOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        var safeLog = context.CreateReplaySafeLogger(log);
        safeLog.LogError("STARTED PATTERN CHAIN");
        
        var outputs = new List<string>();

        outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
        outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
        outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

        safeLog.LogError("FINISHED PATTERN CHAIN");

        return outputs;
    }

    [FunctionName(nameof(SayHello))]
    public static string SayHello([ActivityTrigger] string name, ILogger log)
    {
        log.LogWarning("Saying hello to {name}.", name);

        return $"Hello {name}!";
    }
}
