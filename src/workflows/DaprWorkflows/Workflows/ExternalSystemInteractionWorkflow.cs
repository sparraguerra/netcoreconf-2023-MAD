using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;

namespace DaprWorkflows.Workflows;

public class ExternalSystemInteractionWorkflow : Workflow<string, bool>
{
    private readonly WorkflowTaskOptions retryOptions = new()
    {
        RetryPolicy = new WorkflowRetryPolicy(
                        firstRetryInterval: TimeSpan.FromMinutes(1),
                        backoffCoefficient: 2.0,
                        maxRetryInterval: TimeSpan.FromHours(1),
                        maxNumberOfAttempts: 10),
    };

    public override async Task<bool> RunAsync(WorkflowContext context, string payload)
    {
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("Processing external system interaction workflow")); 
        var message = string.Empty;
        try
        {
            var timeOut = TimeSpan.FromSeconds(20);
            var approvalEvent = await context.WaitForExternalEventAsync<ApprovalEvent>(Constants.ApprovalEventName, TimeSpan.FromSeconds(20));

            if (approvalEvent.IsApproved)
            {
                await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("ApprovalEvent is approved!"), retryOptions);
            }
            else
            {
                await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("ApprovalEvent is rejected!"), retryOptions);
            }
        }
        catch (TaskCanceledException)
        {
            context.SetCustomStatus("Wait for external event is cancelled due to timeout.");
        }

        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification("External system interaction workflow completed!"));
        return true;
    }
}

