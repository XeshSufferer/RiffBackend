using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using RiffCore.Models;
using RiffCore.Services;
using RiffCore.Tracker;

var builder = WebApplication.CreateBuilder(args);

await Task.Delay(3000);

builder.Services.AddOpenApi();


// DI
builder.Services.AddSingleton<IRabbitMQService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<RabbitMQService>>();
    var service = new RabbitMQService("rabbitmq", 5672, "guest", "guest", logger);
    return service;
});
builder.Services.AddSingleton<IUniversalRequestTracker, UniversalRequestTracker>();
builder.Services.AddSingleton<IJWTService, JWTService>(p => new JWTService(
    "keykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykey", "server", "client"));


// Auth Options

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            ValidateIssuer = true,
            ValidIssuer = "server",
            ValidateAudience = true,
            ValidAudience = "client",
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("keykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykeykey")),
            ValidateIssuerSigningKey = true,
        };
    });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();


// Runtime

var rabbitService = app.Services.GetRequiredService<IRabbitMQService>();
var tracker = app.Services.GetRequiredService<IUniversalRequestTracker>();
var logger = app.Services.GetRequiredService<ILogger<RabbitMQService>>();
var jwtService = app.Services.GetRequiredService<IJWTService>();



await rabbitService.InitializeAsync();

await rabbitService.StartConsumingAsync<User>("Riff.Core.Accounts.Output.Register", message =>
{
    //logger.LogInformation("Riff.Core.Accounts.Output.Register received");
    //logger.LogInformation("output correlationid {id}", message.CorrelationId);
    tracker.TrySetResult(message.CorrelationId, message);
    //logger.LogInformation("Response processed correct. {CorrelationId}", message.CorrelationId);
    return Task.CompletedTask;
});

await rabbitService.StartConsumingAsync<User>("Riff.Core.Accounts.Login.Output", message =>
{
    //logger.LogInformation("Riff.Core.Accounts.Output.Register received");
    //logger.LogInformation("output correlationid {id}", message.CorrelationId);
    tracker.TrySetResult(message.CorrelationId, message);
    //logger.LogInformation("Response processed correct. {CorrelationId}", message.CorrelationId);
    return Task.CompletedTask;
});


app.MapGet("/reg", async () =>
{
    string correlationId = tracker.CreatePendingRequest();
    await rabbitService.SendMessageAsync<string>(correlationId, "Riff.Core.Accounts.Input.Register");
    //logger.LogInformation("input correlationid {id}", correlationId);
    
    
    var endedData = await tracker.WaitForResponseAsync<User>(correlationId);
    var token = jwtService.GenerateToken(endedData.Id);
    return token;
});

app.MapGet("/login", async () =>
{
    string correlationId = tracker.CreatePendingRequest();
    UserLoginData data = new UserLoginData()
    {
        CorrelationId = correlationId,
        Login = "loginlogin",
        Password = "passpass" 
    };
    await rabbitService.SendMessageAsync<UserLoginData>(data, "Riff.Core.Accounts.Input.Login");
    
    
    var userdata = await tracker.WaitForResponseAsync<User>(correlationId);
    return userdata;
});

app.Run();