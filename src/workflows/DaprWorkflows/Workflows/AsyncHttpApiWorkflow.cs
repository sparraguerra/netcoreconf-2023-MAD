using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;

namespace DaprWorkflows.Workflows;

public class AsyncHttpApiWorkflow : Workflow<string, bool>
{
    public override async Task<bool> RunAsync(WorkflowContext context, string payload)
    {
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Processing async workflow"));
        context.SetCustomStatus("Processing");

        await context.CreateTimer(TimeSpan.FromSeconds(15));

        context.SetCustomStatus("Processing completed");
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Async workflow completed!"));

        return true;
    }
}

