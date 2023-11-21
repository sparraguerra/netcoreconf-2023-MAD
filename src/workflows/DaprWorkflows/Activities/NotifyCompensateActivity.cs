using Dapr.Workflow;
using DaprWorkflows.Models;

namespace DaprWorkflows.Activities;

public class NotifyCompensateActivity : WorkflowActivity<Notification, object?>
{
    private readonly ILogger _logger;

    public NotifyCompensateActivity(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<NotifyCompensateActivity>();
    }

    public override Task<object?> RunAsync(WorkflowActivityContext context, Notification notification)
    {
        _logger.LogInformation($"Compensation applied: {notification.Message}");
        return Task.FromResult<object?>(null);
    }
}