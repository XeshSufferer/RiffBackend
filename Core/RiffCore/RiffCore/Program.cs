using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RiffCore.Hubs;
using RiffCore.Services;
using RiffCore.Tracker;
using RiffCore.Сache;

var builder = WebApplication.CreateBuilder(args);

await Task.Delay(4000);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
     {
         options.AddPolicy("Cors",
             builder => builder.AllowAnyOrigin()
                               .AllowCredentials()
                               .AllowAnyMethod()
                               .AllowAnyHeader());
     });


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{builder.Configuration["Redis:ConnectionString"]},abortConnect=false";
    options.InstanceName = "RiffCore";
});


// DI
builder.Services.AddSingleton<IRabbitMQService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<RabbitMQService>>();
    var service = new RabbitMQService(builder.Configuration["RabbitMQ:Host"], 
    int.Parse(builder.Configuration["RabbitMQ:Port"]), 
    builder.Configuration["RabbitMQ:Username"], 
    builder.Configuration["RabbitMQ:Password"], logger);
    return service;
});
builder.Services.AddSingleton<IUniversalRequestTracker, UniversalRequestTracker>();
builder.Services.AddSingleton<IJWTService, JWTService>(p => new JWTService(
        builder.Configuration["JWT:SecretKey"], 
        builder.Configuration["JWT:Issuer"], 
        builder.Configuration["JWT:Audience"]));
builder.Services.AddSingleton<IGatewayRabbitConsumer, GatewayRabbitConsumer>();
builder.Services.AddSingleton<ICacheService, RedisService>();

// Auth Options

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])),
            ValidateIssuerSigningKey = true,
        };
    });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Cors");
//app.UseHttpsRedirection();


// Runtime

var rabbitService = app.Services.GetRequiredService<IRabbitMQService>();
var tracker = app.Services.GetRequiredService<IUniversalRequestTracker>();
var jwtService = app.Services.GetRequiredService<IJWTService>();

// Rabbit services init
await rabbitService.InitializeAsync();

app.MapHub<GatewayHub>("api/gateway");

app.MapGet("/health", () => "OK");


await app.Services.GetRequiredService<IGatewayRabbitConsumer>().StartConsume();

app.Run();