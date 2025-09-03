namespace RiffCore.Services;

public interface IRabbitMQService
{
    Task StartConsumingAsync<T>(string queueName, Func<T, Task> messageHandler,
        CancellationToken cancellationToken = default);

    Task SendMessageAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
    Task EnsureQueueExistsAsync(string queueName);
    Task InitializeAsync();
}