using System.Net;
using RabbitMQ.Client;
using RiffCore.Models;
using RiffCore.Services;
using RiffCore.Tracker;

var builder = WebApplication.CreateBuilder(args);

await Task.Delay(3000);

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
var tracker = app.Services.GetRequiredService<IUniversalRequestTracker>();
var logger = app.Services.GetRequiredService<ILogger<RabbitMQService>>();

//logger.LogInformation("Initialized");


await rabbitService.InitializeAsync();
await rabbitService.StartConsumingAsync<TestMessage>("output", message =>
{
    tracker.TrySetResult(message.CorrelationId, message);
    return Task.CompletedTask;
});

await rabbitService.StartConsumingAsync<User>("Riff.Core.Accounts.Output.Register", message =>
{
    //logger.LogInformation("Riff.Core.Accounts.Output.Register received");
    //logger.LogInformation("output correlationid {id}", message.CorrelationId);
    tracker.TrySetResult(message.CorrelationId, message);
    //logger.LogInformation("Response processed correct. {CorrelationId}", message.CorrelationId);
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


app.MapGet("/reg", async () =>
{
    string correlationId = tracker.CreatePendingRequest();
    await rabbitService.SendMessageAsync<string>(correlationId, "Riff.Core.Accounts.Input.Register");
    //logger.LogInformation("input correlationid {id}", correlationId);
    
    
    var endedData = await tracker.WaitForResponseAsync<User>(correlationId);
    return endedData;
});

app.Run();
struct TestMessage
{
    public object Message { get; set; }
    public string CorrelationId { get; set; }
}