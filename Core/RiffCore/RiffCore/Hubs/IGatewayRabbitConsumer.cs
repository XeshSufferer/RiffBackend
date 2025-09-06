namespace RiffCore.Hubs;

public interface IGatewayRabbitConsumer
{
    Task StartConsume();
}