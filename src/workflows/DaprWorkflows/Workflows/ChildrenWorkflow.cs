using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;

namespace DaprWorkflows.Workflows;

public class ChildrenWorkflow : Workflow<WorkflowPayload, bool>
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
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"Executing children workflows {nameof(TaskChainingWorkflow)} {nameof(FanOutFanInWorkflow)}"), retryOptions);

        var chaining = await context.CallChildWorkflowAsync<bool>(nameof(TaskChainingWorkflow), payload);
 
        var fanOutFanIn = await context.CallChildWorkflowAsync<bool>(nameof(FanOutFanInWorkflow), payload);

        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Children workflows completed!"), retryOptions);

        return chaining && fanOutFanIn;
    }
}

