using RiffCore.Models;
using RiffCore.Services;
using RiffCore.Tracker;

namespace RiffCore.Hubs;

public class GatewayRabbitConsumer : IGatewayRabbitConsumer
{
    private readonly IUniversalRequestTracker _tracker;
    private readonly IRabbitMQService _rabbit;
    private readonly ILogger<GatewayRabbitConsumer> _logger;

    private bool _isConsume = false;

    public GatewayRabbitConsumer(IRabbitMQService rabbit, IUniversalRequestTracker tracker, ILogger<GatewayRabbitConsumer> logger)
    {
        _tracker = tracker;
        _rabbit = rabbit;
        _logger = logger;
    }

    public async Task StartConsume()
    {
        if (_isConsume) return;
        
        await _rabbit.StartConsumingAsync<User>("Riff.Core.Accounts.Register.Output", message =>
        {
            _tracker.TrySetResult(message.CorrelationId, message);
            return Task.CompletedTask;
        });
        
        await _rabbit.StartConsumingAsync<User>("Riff.Core.Accounts.Login.Output", message =>
        {
            
            _tracker.TrySetResult(message.CorrelationId, message);
            return Task.CompletedTask;
        });
        
        await _rabbit.StartConsumingAsync<User>("Riff.Core.Accounts.GetByID.Output", message =>
        {
            _tracker.TrySetResult(message.CorrelationId, message);
            return Task.CompletedTask;
        });

        await _rabbit.StartConsumingAsync<ChatCreatingAcceptDTO>("Riff.Core.Accounts.CreateChat.Output", accept =>
        {
            _tracker.TrySetResult(accept.CorrelationId, accept);
            return Task.CompletedTask;
        });
        
        _isConsume = true;
    }
    
    
    
}