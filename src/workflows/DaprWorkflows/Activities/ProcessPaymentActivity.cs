using Dapr.Workflow;
using DaprWorkflows.Models;

namespace DaprWorkflows.Activities;

public class ProcessPaymentActivity : WorkflowActivity<PaymentRequest, PaymentResponse>
{
    private readonly ILogger _logger;

    public ProcessPaymentActivity(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ProcessPaymentActivity>();
    }

    public override async Task<PaymentResponse> RunAsync(WorkflowActivityContext context, PaymentRequest req)
    {
        _logger.LogError("Processing payment: {@PaymentRequest} ", req);
        // Simulate slow processing for Demos
        await Task.Delay(TimeSpan.FromSeconds(5));
        _logger.LogError("Payment processed");

        return new PaymentResponse(true);
    }
}