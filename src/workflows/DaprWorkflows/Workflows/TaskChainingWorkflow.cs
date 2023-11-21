using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;

namespace DaprWorkflows.Workflows;

public class TaskChainingWorkflow : Workflow<WorkflowPayload, bool>
{
    private readonly WorkflowTaskOptions retryOptions = new()
    {
        RetryPolicy = new WorkflowRetryPolicy(
                        firstRetryInterval: TimeSpan.FromMinutes(1),
                        backoffCoefficient: 2.0,
                        maxRetryInterval: TimeSpan.FromHours(1),
                        maxNumberOfAttempts: 10),
    };

    public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
    {
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Processing task chaining workflow"), retryOptions);

        await context.CallActivityAsync<object?>(nameof(DelayActivity), new Notification($"{context.InstanceId} - Activity #1"), retryOptions);
        await context.CallActivityAsync<object?>(nameof(DelayActivity), new Notification($"{context.InstanceId} - Activity #2"), retryOptions);
        await context.CallActivityAsync<object?>(nameof(DelayActivity), new Notification($"{context.InstanceId} - Activity #3"), retryOptions);

        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Task chaining completed!"), retryOptions);

        return true;
    }
}