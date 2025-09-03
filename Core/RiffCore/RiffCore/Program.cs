using System.Net;
using RabbitMQ.Client;
using RiffCore.Services;

var builder = WebApplication.CreateBuilder(args);

await Task.Delay(1500);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IRabbitMQService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<RabbitMQService>>();
    var service = new RabbitMQService("rabbitmq", 5672, "guest", "guest", logger);
    return service;
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var rabbitService = app.Services.GetRequiredService<IRabbitMQService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

await rabbitService.InitializeAsync();
await rabbitService.StartConsumingAsync<string>("output", message =>
{
    logger.LogInformation("Received: {message}", message);
    return Task.CompletedTask;
});




app.MapGet("/", () =>
{
    rabbitService.SendMessageAsync("Hello world!", "input");
    return "Hello world!";
});

app.Run();