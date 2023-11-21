using Dapr.Workflow;
using DaprWorkflows.Models;

namespace DaprWorkflows.Activities;

public class NotifyActivity : WorkflowActivity<Notification, object?>
{
    private readonly ILogger _logger;

    public NotifyActivity(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<NotifyActivity>();
    }

    public override Task<object?> RunAsync(WorkflowActivityContext context, Notification notification)
    {
        _logger.LogInformation(notification.Message);
        return Task.FromResult<object?>(null);
    }
}