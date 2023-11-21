using Dapr.Workflow;
using DaprWorkflows.Activities;
using DaprWorkflows.Models;
using DaprWorkflows.Workflows;
using Serilog;
using Serilog.Events;
using StartWorkflowResponse = DaprWorkflows.Models.StartWorkflowResponse;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

builder.Services.AddDaprClient();

builder.Services.AddDaprWorkflow(options =>
{
    // Note that it's also possible to register a lambda function as the workflow
    // or activity implementation instead of a class.
    options.RegisterWorkflow<CheckoutWorkflow>();
    options.RegisterWorkflow<TaskChainingWorkflow>();
    options.RegisterWorkflow<FanOutFanInWorkflow>();
    options.RegisterWorkflow<AsyncHttpApiWorkflow>();
    options.RegisterWorkflow<MonitorWorkflow>();
    options.RegisterWorkflow<ChildrenWorkflow>();
    options.RegisterWorkflow<TimerWorkflow>();
    options.RegisterWorkflow<ExternalSystemInteractionWorkflow>();

    // These are the activities that get invoked by the workflow(s).
    options.RegisterActivity<NotifyActivity>();
    options.RegisterActivity<CheckInventoryActivity>();
    options.RegisterActivity<ProcessPaymentActivity>();
    options.RegisterActivity<RefundPaymentActivity>();
    options.RegisterActivity<UpdateInventoryActivity>();
    options.RegisterActivity<NotifyCompensateActivity>();
    options.RegisterActivity<DelayActivity>();
});

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region Dapr CheckOutWorkflow

string storeName = "inventorystore";
string[] itemKeys = new[] { "item1", "item2" };

app.MapGet("/inventory", async (Dapr.Client.DaprClient _client, ILogger<Program> _logger) =>
{
    var inventory = new List<InventoryItem>();
    
    foreach (var itemKey in itemKeys)
    {
        var item = await _client.GetStateAsync<InventoryItem>(storeName, itemKey.ToLowerInvariant());
        inventory.Add(item);
    }
    _logger.LogInformation("Inventory Retrieved!");

    return Results.Ok(inventory);
})
.WithName("GetInventory");

app.MapPost("/inventory/restock", async (Dapr.Client.DaprClient _client, ILogger<Program> _logger) =>
{
    var baseInventory = new List<InventoryItem>
    {
        new(ProductId: 0, Name: itemKeys[0], PerItemCost: 20, Quantity: 100),
        new(ProductId: 1, Name: itemKeys[1], PerItemCost: 20, Quantity: 100),
    };

    foreach (var item in baseInventory)
    {
        await _client.SaveStateAsync(storeName, item.Name.ToLowerInvariant(), item);
    }

    _logger.LogInformation("Inventory Restocked!");

    return Results.Ok();
})
.WithName("RestockInventory");

app.MapPost("/check-out-workflow", async (CustomerOrder order, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    var instanceId = Guid.NewGuid().ToString();

    var schedule = await _workflowClient.ScheduleNewWorkflowAsync(
                                           name: nameof(CheckoutWorkflow),
                                           input: order,
                                           instanceId: instanceId);

    _logger.LogInformation("Workflow: {CheckoutWorkflow} (ID = {InstanceId}) started successfully.",
                          nameof(CheckoutWorkflow),
                          instanceId); 

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("ProcessOrder")
.Produces<StartWorkflowResponse>();

#endregion

#region Dapr TaskChainingWorkflow

app.MapPost("/task-chaining-workflow", async (Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    var instanceId = Guid.NewGuid().ToString();

    var schedule = await _workflowClient.ScheduleNewWorkflowAsync(
                                    name: nameof(TaskChainingWorkflow),
                                    input: new WorkflowPayload(Guid.NewGuid().ToString(), 0),
                                    instanceId: instanceId);

    _logger.LogInformation("Workflow: {TaskChainingWorkflow} (ID = {InstanceId}) started successfully.",
                           nameof(TaskChainingWorkflow),
                           instanceId); 

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("TaskChaining")
.Produces<StartWorkflowResponse>();

#endregion

#region Dapr FanOutFanInWorkflow

app.MapPost("/fan-out-workflow", async (int count, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    var instanceId = Guid.NewGuid().ToString();

    var schedule = await _workflowClient.ScheduleNewWorkflowAsync(
                                    name: nameof(FanOutFanInWorkflow),
                                    input: new WorkflowPayload(Guid.NewGuid().ToString(), count),
                                    instanceId: instanceId);

    _logger.LogInformation("Workflow: {FanOutFanInWorkflow} (ID = {InstanceId}) started successfully.",
                           nameof(FanOutFanInWorkflow),
                           instanceId);

    return Results.Ok(new { instanceId });
})
.WithName("FanOut")
.Produces<StartWorkflowResponse>();

#endregion

#region Dapr AsyncHttpApiWorkflow

app.MapPost("/async-http-workflow/start", async (Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    using var cts = new CancellationTokenSource();

    var instanceId = await _workflowClient.ScheduleNewWorkflowAsync(
                                   name: nameof(AsyncHttpApiWorkflow),
                                   input: Guid.NewGuid().ToString());

    _logger.LogInformation("Workflow: {AsyncHttpApiWorkflow} (ID = {InstanceId}) started successfully.",
                           nameof(AsyncHttpApiWorkflow),
                           instanceId);

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("AsyncHttpStart")
.Produces<StartWorkflowResponse>();

app.MapPost("/async-http-workflow/status", async (string instanceId, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }
    using var cts = new CancellationTokenSource();
    var state = await _client.GetWorkflowAsync(instanceId: instanceId, workflowComponent: "dapr", cancellationToken: cts.Token);

    return Results.Ok(state); 
})
.WithName("AsyncHttpStatus")
.Produces<WorkflowState>();

#endregion

#region Dapr ChildrenWorkflow

app.MapPost("/children-workflow", async (int count, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }
    var instanceId = Guid.NewGuid().ToString();

    var schedule = await _workflowClient.ScheduleNewWorkflowAsync(
                                           name: nameof(ChildrenWorkflow),
                                           input: new WorkflowPayload(Guid.NewGuid().ToString(), count),
                                           instanceId: instanceId);

    _logger.LogInformation("Workflow: {ChildrenWorkflow} (ID = {InstanceId}) started successfully.",
                          nameof(ChildrenWorkflow),
                          instanceId);

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("Children")
.Produces<StartWorkflowResponse>();

#endregion

#region Dapr MonitorWorkflow

app.MapPost("/monitor-workflow", async (int count, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }
    var instanceId = Guid.NewGuid().ToString();

    var schedule = await _workflowClient.ScheduleNewWorkflowAsync(
                                           name: nameof(MonitorWorkflow),
                                           input: count,
                                           instanceId: instanceId);

    _logger.LogInformation("Workflow: {MonitorWorkflow} (ID = {InstanceId}) started successfully.",
                          nameof(MonitorWorkflow),
                          instanceId);

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("Monitor")
.Produces<StartWorkflowResponse>();

#endregion

#region Dapr TimerWorkflow

app.MapPost("/timer-workflow", async (TimerWorkflowPayload payload, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }
    var instanceId = Guid.NewGuid().ToString();

    var schedule = await _workflowClient.ScheduleNewWorkflowAsync(
                                           name: nameof(TimerWorkflow),
                                           input: payload,
                                           instanceId: instanceId);

    _logger.LogInformation("Workflow: {TimerWorkflow} (ID = {InstanceId}) started successfully.",
                          nameof(TimerWorkflow),
                          instanceId);

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("Timer")
.Produces<StartWorkflowResponse>();

#endregion

#region Dapr ExternalSystemInteractionWorkflow

app.MapPost("/external-system-interaction-workflow/start", async (Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    using var cts = new CancellationTokenSource();

    var instanceId = await _workflowClient.ScheduleNewWorkflowAsync(
                                   name: nameof(ExternalSystemInteractionWorkflow),
                                   input: Guid.NewGuid().ToString());

    _logger.LogInformation("Workflow: {ExternalSystemInteractionWorkflow} (ID = {InstanceId}) started successfully.",
                           nameof(ExternalSystemInteractionWorkflow),
                           instanceId);

    return Results.Ok(new StartWorkflowResponse(instanceId));
})
.WithName("ExternalSystemInteractionStart")
.Produces<StartWorkflowResponse>();

app.MapPost("/external-system-interaction-workflow/raise", async (string instanceId, bool approve, Dapr.Client.DaprClient _client, DaprWorkflowClient _workflowClient, ILogger<Program> _logger) =>
{
    while (!await _client.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }
   
    await _client.RaiseWorkflowEventAsync(instanceId, "dapr", Constants.ApprovalEventName, new ApprovalEvent(approve));

    return Results.Ok();
})
.WithName("ExternalSystemInteractionRaise")
.Produces<WorkflowState>();

#endregion

await app.RunAsync();