using Dapr.Workflow;
using DaprWorkflows.Models;

namespace DaprWorkflows.Activities;

public class DelayActivity : WorkflowActivity<Notification, object?>
{
    private readonly ILogger _logger;

    public DelayActivity(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DelayActivity>();
    }

    public override async Task<object?> RunAsync(WorkflowActivityContext context, Notification notification)
    {
        _logger.LogInformation("waiting..." + notification.Message);
        
        await Task.Delay(3000);

        _logger.LogInformation("finished. " + notification.Message);

        return Task.FromResult<object?>(null);
    }
}