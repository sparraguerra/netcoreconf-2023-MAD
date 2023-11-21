using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;
using DurableTask.Core.Exceptions;

namespace DaprWorkflows.Workflows;

public class CheckoutWorkflow : Workflow<CustomerOrder, CheckoutResult>
{
    private readonly WorkflowTaskOptions retryOptions = new()
    {
        RetryPolicy = new WorkflowRetryPolicy(
                        firstRetryInterval: TimeSpan.FromMinutes(1),
                        backoffCoefficient: 2.0,
                        maxRetryInterval: TimeSpan.FromHours(1),
                        maxNumberOfAttempts: 10),
    };

    public override async Task<CheckoutResult> RunAsync(WorkflowContext context, CustomerOrder order)
    {
        string orderId = context.InstanceId;

        // Order Received 
        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"Received order {orderId} for {order.OrderItem.Quantity} {order.OrderItem.Name}"), retryOptions);

        // Check Product Inventory
        context.SetCustomStatus("Checking product inventory");

        var inventoryResult = await context.CallActivityAsync<InventoryResult>(nameof(CheckInventoryActivity), order, retryOptions);

        if (!inventoryResult.Available)
        {
            // End the workflow here since we don't have sufficient inventory
            await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"{orderId} cancelled: Insufficient inventory available"), retryOptions);

            context.SetCustomStatus("Insufficient inventory to fulfill order");

            return new CheckoutResult(Processed: false);
        }

        var paymentRequest = new PaymentRequest(RequestId: orderId, order.Name, order.OrderItem.Name, inventoryResult.TotalCost);
        // Process payment for the order 
        try
        {
            context.SetCustomStatus("Payment processing");

            var paymentResponse = await context.CallActivityAsync<PaymentResponse>(nameof(ProcessPaymentActivity), paymentRequest, retryOptions);

            if (!paymentResponse.Success)
            {
                // End the workflow here since we were unable to process payment 
                await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"{orderId} cancelled: Payment processing failed"));

                context.SetCustomStatus("Payment failed");

                return new CheckoutResult(Processed: false);
            }
        }
        catch (Exception ex)
        {
            if (ex.InnerException is TaskFailedException)
            {
                await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"Processing payment for {orderId} failed due to {ex.Message}"));
                context.SetCustomStatus("Payment failed to process");
                return new CheckoutResult(Processed: false);
            }
        }

        // Decrement inventory to account for execution of purchase 
        try
        {
            await context.CallActivityAsync<object?>(nameof(UpdateInventoryActivity), order, retryOptions);

            context.SetCustomStatus("Updating inventory as a result of order payment");
        }
        catch (Exception ex)
        {
            if (ex.InnerException is TaskFailedException)
            {
                await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Models.Notification($"Checkout for order {orderId} failed! Processing payment refund."));

                context.SetCustomStatus("Issuing refund due to insufficient inventory to fulfill");

                await context.CallActivityAsync<PaymentResponse>(nameof(ProcessPaymentActivity), paymentRequest, retryOptions);

                context.SetCustomStatus("Payment refunded");

                return new CheckoutResult(Processed: false);
            }
        }

        await context.CallActivityAsync<object?>(nameof(NotifyActivity), new Notification($"Checkout for order {orderId} has completed!"), retryOptions);

        context.SetCustomStatus("Checkout completed");

        return new CheckoutResult(Processed: true);
    }
}
