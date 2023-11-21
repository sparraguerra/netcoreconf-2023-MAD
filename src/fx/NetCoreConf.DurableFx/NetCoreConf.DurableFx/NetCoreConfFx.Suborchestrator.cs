using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetCoreConf.DurableFx;

public partial class NetCoreConfFx
{
    [FunctionName("SuborchestratorFX_Orchestrator")]
    public static async Task SuborchestratorFXOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        //log.LogDebug(log.ToString())
        var safeLog = context.CreateReplaySafeLogger(log);
        safeLog.LogError("STARTED NESTED ORCHESTRATIONS");

        var outputs = new List<string>();

        await context.CallSubOrchestratorAsync<List<string>>("ChainFx_Orchestrator", null);
        await context.CallSubOrchestratorAsync("FanOutFanInFx_Orchestrator", null);

        safeLog.LogError("FINISHED NESTED ORCHESTRATIONS");
    }
}
