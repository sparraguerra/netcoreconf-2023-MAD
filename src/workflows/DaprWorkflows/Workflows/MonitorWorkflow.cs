using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;

namespace DaprWorkflows.Workflows;

public class MonitorWorkflow : Workflow<int, bool>
{
    private readonly WorkflowTaskOptions retryOptions = new()
    {
        RetryPolicy = new WorkflowRetryPolicy(
                        firstRetryInterval: TimeSpan.FromMinutes(1),
                        backoffCoefficient: 2.0,
                        maxRetryInterval: TimeSpan.FromHours(1),
                        maxNumberOfAttempts: 10),
    };

    public override async Task<bool> RunAsync(WorkflowContext context, int counter)
    {
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"{context.InstanceId} - Monitor Activity #{counter}"), retryOptions); 

        if (counter < 10)
        {
            counter += 1;
            await context.CreateTimer(TimeSpan.FromSeconds(1));
            context.ContinueAsNew(counter);
        }

        return true;
    }
}

