namespace DaprWorkflows.Models;

// Orders
public record OrderItem(string Name, int Quantity);
public record CustomerOrder(string Name, OrderItem OrderItem);

// Inventory
public record InventoryItem(int ProductId, string Name, int PerItemCost, int Quantity);
public record InventoryResult(bool Available, InventoryItem? ProductItem, int TotalCost);

// Payment 
public record PaymentRequest(string RequestId, string Name, string OrderItem, int TotalCost);
public record PaymentResponse(bool Success);
public record CheckoutResult(bool Processed);

// Notifications
public record Notification(string Message);

// Workflow Patterns
public record StartWorkflowResponse(string InstanceId);
 
public record WorkflowPayload(string RandomData, int Count = 0);

public record TimerWorkflowPayload(string Name, DateTime DateTime);
 
public record ApprovalEvent(bool IsApproved); 
public static class Constants
{
    public static readonly string ApprovalEventName = "ApprovalEvent";
}