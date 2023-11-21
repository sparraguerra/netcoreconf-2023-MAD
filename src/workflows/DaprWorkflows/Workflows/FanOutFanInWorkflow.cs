using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;
using DurableTask.Core;
using System;

namespace DaprWorkflows.Workflows;

public class FanOutFanInWorkflow : Workflow<WorkflowPayload, bool>
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
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Processing fan out workflow"), retryOptions);

        var parallelTasks = new List<Task<object?>>(payload.Count);
        for (int index = 0; index < payload.Count; index++)
        {
            var task = context.CallActivityAsync<object?>(nameof(DelayActivity), new Notification($"{context.InstanceId} - Activity #{index}"), retryOptions);
            parallelTasks.Add(task);
        }

        // Everything is scheduled. Wait here until all parallel tasks have completed.
        await Task.WhenAll(parallelTasks);

        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("FanOut FanIn completed!"), retryOptions);

        return true;
    }
}