using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using TaskEventListener.Configuration;

namespace TaskEventListener.Services;

public class RabbitMqListenerService : BackgroundService
{
    private readonly ILogger<RabbitMqListenerService> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "tasks";
    private const string QueueName = "task_events_log_queue";
    private readonly AsyncRetryPolicy _connectionRetryPolicy;

    public RabbitMqListenerService(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqListenerService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        _connectionRetryPolicy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<ConnectFailureException>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "RabbitMQ connection failed. Retrying connection in {TimeSpan}. Attempt {RetryCount}/5",
                        timeSpan,
                        retryCount
                    );
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Listener Service is starting.");

        stoppingToken.Register(() =>
            _logger.LogInformation("RabbitMQ Listener Service is stopping."));

        await ConnectWithRetryAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_connection is not { IsOpen: true } || _channel is not { IsOpen: true })
            {
                _logger.LogWarning("Connection or channel lost. Attempting to reconnect...");
                await ConnectWithRetryAsync(stoppingToken);
                await Task.Delay(5000, stoppingToken);
                continue;
            }
            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        _logger.LogInformation("Cancellation requested. Shutting down listener.");
        await DisposeConnection();
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        await _connectionRetryPolicy.ExecuteAsync(async (token) =>
        {
            await Task.Run(() =>
            {
                Connect(token);
            }, token);
        }, stoppingToken);
        
        if (_channel is { IsOpen: true })
        {
            SetupConsumer(stoppingToken);
        }
        else
        {
            _logger.LogError("Failed to establish RabbitMQ connection after retries.");
        }
    }

    private void Connect(CancellationToken stoppingToken)
    {
        if (_connection is { IsOpen: true }) return;
        
        _logger.LogInformation("Attempting to connect to RabbitMQ host: {HostName}", _settings.HostName);
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName ?? throw new InvalidOperationException(),
                Password = _settings.Password ?? throw new InvalidOperationException(),
                VirtualHost = _settings.VirtualHost ?? throw new InvalidOperationException()
            };
            _connection = _connectionRetryPolicy.ExecuteAsync(() => factory.CreateConnectionAsync(stoppingToken)).Result;
            _channel = _connection.CreateChannelAsync(cancellationToken: stoppingToken).Result;

            _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: stoppingToken
            );
            _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken
            );

            _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: "task.*",
                cancellationToken: stoppingToken
            );

            _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            _connection.ConnectionShutdownAsync += OnConnectionShutdown;
            _logger.LogInformation(
                "Successfully connected to RabbitMQ, channel opened, queue '{QueueName}' declared and bound.",
                QueueName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ.");
            DisposeConnection();
            throw;
        }
    }

    private void SetupConsumer(CancellationToken stoppingToken)
    {
        if (_channel == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = "Unknown";
            var taskId = "Unknown";

            try
            {
                using (var document = JsonDocument.Parse(message))
                {
                    if (document.RootElement.TryGetProperty("EventType", out var eventTypeElement))
                    {
                        eventType = eventTypeElement.GetString() ?? "Unknown";
                    }
                    if (document.RootElement.TryGetProperty("Payload", out var payloadElement))
                    {
                        if (payloadElement.TryGetProperty("Id", out var idElement))
                        {
                            taskId = idElement.TryGetInt32(out var id) ? id.ToString() : "NonInt";
                        } else if (payloadElement.TryGetProperty("TaskId", out var taskIdElement))
                        {
                            taskId = taskIdElement.TryGetInt32(out int tid) ? tid.ToString() : "NonInt";
                        }
                    }
                }

                _logger.LogInformation(
                    "Received event '{EventType}' for Task ID '{TaskId}'. RoutingKey: '{RoutingKey}'. Message: {Message}",
                    eventType,
                    taskId,
                    ea.RoutingKey,
                    message
                );
                
                _channel?.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(
                    jsonEx,
                    "Failed to parse JSON message. RoutingKey: '{RoutingKey}', Message: {Message}",
                    ea.RoutingKey,
                    message
                );
                _channel?.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing message for event '{EventType}', Task ID '{TaskId}'. RoutingKey: '{RoutingKey}'. Message: {Message}",
                    eventType,
                    taskId,
                    ea.RoutingKey,
                    message
                );
                _channel?.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken
                );
            }

            return Task.CompletedTask;
        };

        _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );
        _logger.LogInformation("Consumer started listening on queue '{QueueName}'.", QueueName);
    }

    private async Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shut down: {ReplyText}. Listener will attempt to reconnect.", e.ReplyText);
        await DisposeConnection();
    }

    private Task DisposeConnection()
    {
        try
        {
            _channel?.CloseAsync();
            _channel?.Dispose();
            _channel = null;
        }
        catch (Exception ex) { _logger.LogError(ex, "Error disposing RabbitMQ channel.");}
        
        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                _connection.CloseAsync();
                _connection.Dispose();
                _connection = null;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error disposing RabbitMQ connection.");}
        
        return Task.CompletedTask;
    }
    
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Listener Service is stopping.");
        await DisposeConnection();
        await base.StopAsync(stoppingToken);
    }
}