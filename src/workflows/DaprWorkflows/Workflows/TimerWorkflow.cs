using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;

namespace DaprWorkflows.Workflows;

public class TimerWorkflow : Workflow<TimerWorkflowPayload, bool>
{
    private readonly WorkflowTaskOptions retryOptions = new()
    {
        RetryPolicy = new WorkflowRetryPolicy(
                        firstRetryInterval: TimeSpan.FromMinutes(1),
                        backoffCoefficient: 2.0,
                        maxRetryInterval: TimeSpan.FromHours(1),
                        maxNumberOfAttempts: 10),
    };

    public override async Task<bool> RunAsync(WorkflowContext context, TimerWorkflowPayload payload)
    {
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Processing timer workflow"), retryOptions);
        context.SetCustomStatus("Processing");

        if (payload.DateTime.ToUniversalTime() > context.CurrentUtcDateTime)
        {
            context.SetCustomStatus($"Waiting for timer: {payload.DateTime:yyyy-MM-dd HH:mm:ss}");
            await context.CreateTimer(payload.DateTime, default);
        }

        context.SetCustomStatus(null);
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"Terminated {payload.Name} at {payload.DateTime:yyyy-MM-dd HH:mm:ss}"), retryOptions);
      
        return true;
    }
}

