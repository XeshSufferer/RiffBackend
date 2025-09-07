using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RiffCore.Models;
using RiffCore.Services;
using RiffCore.Tracker;

namespace RiffCore.Hubs;

public class GatewayHub : Hub
{
    
    private readonly IJWTService _jwt;
    private readonly ILogger<GatewayHub> _logger;
    private readonly IUniversalRequestTracker  _tracker;
    private readonly IRabbitMQService _rabbit;
    
    public GatewayHub(IJWTService jwt,  ILogger<GatewayHub> logger, IUniversalRequestTracker tracker,  IRabbitMQService rabbit)
    {
        _jwt = jwt;
        _logger = logger;
        _tracker = tracker;
        _rabbit = rabbit;
    }
    
    
    public async Task Login(UserLoginData data)
    {
        _logger.LogInformation($"Login request received {data.Login} {data.Password}");
        var correlationId = _tracker.CreatePendingRequest();
        data.CorrelationId = correlationId;
        _rabbit.SendMessageAsync<UserLoginData>(data, "Riff.Core.Accounts.Login.Input");
        var userdata = await _tracker.WaitForResponseAsync<User>(correlationId);

        if (userdata.PasswordHash == "NULL")
        {
            await Clients.Caller.SendAsync("LoginFailed");
            return;
        }
        
        var token = _jwt.GenerateToken(userdata.Id);
        
        await Clients.Caller.SendAsync("LoginSuccess", token);
    }

    public async Task Register(UserRegisterData data)
    {
        var correlationId = _tracker.CreatePendingRequest();
        data.CorrelationID = correlationId;
        _logger.LogInformation($"Register request received from {data.Nickname}");
        _rabbit.SendMessageAsync<UserRegisterData>(data, "Riff.Core.Accounts.Register.Input");
        var userdata = await _tracker.WaitForResponseAsync<User>(correlationId);

        if (userdata.PasswordHash == "NULL")
        {
            await Clients.Caller.SendAsync("RegistrationFailed");
            return;
        }
        
        var token = _jwt.GenerateToken(userdata.Id);
        await Clients.Caller.SendAsync("RegisterSuccess", token);
    }

    [Authorize]
    public async Task Autologin(string token)
    {
        var correlationId = _tracker.CreatePendingRequest();
        UserIdDTO data = new UserIdDTO
        {
            CorrelationId = correlationId,
            Id = Context.User.Identity.Name,
        };
        _rabbit.SendMessageAsync(data, "Riff.Core.Accounts.GetByID.Input");
        var userdata = await _tracker.WaitForResponseAsync<User>(correlationId);
        
        _logger.LogInformation("Login request received nick: {nick} password: {pass} id: {id}", userdata.Name, userdata.PasswordHash, userdata.Id);
        
        if (userdata.Id == "000000000000000000000000")
        {
            await Clients.Caller.SendAsync("LoginFailed");
            return;
        }
        await Clients.Caller.SendAsync("LoginSuccess", token);
    }
}