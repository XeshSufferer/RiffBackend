using System.Net;
using RabbitMQ.Client;
using RiffCore.Services;
using RiffCore.Tracker;

var builder = WebApplication.CreateBuilder(args);

await Task.Delay(1500);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IRabbitMQService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<RabbitMQService>>();
    var service = new RabbitMQService("rabbitmq", 5672, "guest", "guest", logger);
    return service;
});
builder.Services.AddSingleton<IUniversalRequestTracker, UniversalRequestTracker>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var rabbitService = app.Services.GetRequiredService<IRabbitMQService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var tracker = app.Services.GetRequiredService<IUniversalRequestTracker>();


await rabbitService.InitializeAsync();
await rabbitService.StartConsumingAsync<TestMessage>("output", message =>
{
    logger.LogInformation("Received: {message}", message.Message);
    tracker.TrySetResult(message.CorrelationId, message);
    return Task.CompletedTask;
});




app.MapGet("/", async () =>
{
    string correlationId = tracker.CreatePendingRequest();
    var data = new TestMessage() { Message = "Write it me pls", CorrelationId = correlationId };
    rabbitService.SendMessageAsync<TestMessage>(data, "input");

    var endedData = await tracker.WaitForResponseAsync<TestMessage>(correlationId);
    
    return endedData.Message;
});

app.Run();


struct TestMessage
{
    public string Message { get; set; }
    public string CorrelationId { get; set; }
}