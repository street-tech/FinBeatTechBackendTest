using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Infrastructure.Configuration;

namespace TaskManager.Infrastructure.Messaging;

/// <summary>
/// Implements IMessageProducer using RabbitMQ.
/// </summary>
public sealed class RabbitMqProducer : IMessageProducer, IDisposable
{
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly object _lock = new object();
    private readonly AsyncRetryPolicy _connectionRetryPolicy;


    public RabbitMqProducer(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqProducer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        _connectionRetryPolicy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<ConnectFailureException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "RabbitMQ connection failed. Retrying in {TimeSpan}. Attempt {RetryCount}/3",
                        timeSpan,
                        retryCount
                    );
                });
        
        Connect();
    }

    private void Connect()
    {
        lock (_lock)
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return;
            }

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
                
                _connection = _connectionRetryPolicy.ExecuteAsync(() => factory.CreateConnectionAsync()).Result;
                _channel = _connection.CreateChannelAsync().Result;
                
                _channel.ExchangeDeclareAsync(exchange: "tasks_exchange", type: ExchangeType.Topic, durable: true);

                _connection.ConnectionShutdownAsync += OnConnectionShutdown;
                _connection.CallbackExceptionAsync += OnCallbackException;
                _channel.CallbackExceptionAsync += OnCallbackException;
                _channel.ChannelShutdownAsync += OnChannelShutdown;


                _logger.LogInformation("Successfully connected to RabbitMQ and channel opened.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ.");
            }

        }
    }

    private Task OnChannelShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ channel shut down: {ReplyText}. Attempting to reconnect...", e.ReplyText);
        DisposeChannel();
        Connect();
        return Task.CompletedTask;
    }

    private Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ callback exception.");
        return Task.CompletedTask;
    }

    private async Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shut down: {ReplyText}. Attempting to reconnect...", e.ReplyText);
        await DisposeConnection();
        Connect();
    }

    public Task SendMessageAsync(string exchangeName, string routingKey, string message)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                if (_channel is not { IsOpen: true })
                {
                    _logger.LogWarning("RabbitMQ channel is not open. Attempting to reconnect before sending.");
                    Connect();
                    if (_channel is not { IsOpen: true })
                    {
                        _logger.LogError(
                            "Failed to send message: RabbitMQ channel is unavailable after attempting to reconnect."
                        );
                        throw new InvalidOperationException("RabbitMQ channel is not available.");
                    }
                }
                
                try
                {
                    _channel.ExchangeDeclarePassiveAsync(exchangeName);

                    var body = Encoding.UTF8.GetBytes(message);

                    _logger.LogDebug(
                        "Publishing message to exchange '{ExchangeName}' with routing key '{RoutingKey}'. Size: {MessageSize} bytes",
                        exchangeName,
                        routingKey,
                        body.Length
                    );

                    _channel?.BasicPublishAsync(
                        exchange: exchangeName,
                        routingKey: routingKey,
                        body: body);
                    _logger.LogInformation(
                        "Message published successfully to exchange '{ExchangeName}' with routing key '{RoutingKey}'.",
                        exchangeName,
                        routingKey
                    );


                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish message to RabbitMQ. Exchange: {ExchangeName}, RoutingKey: {RoutingKey}",
                        exchangeName,
                        routingKey
                    );
                    throw;
                }
            }
        });
    }

    public void Dispose()
    {
        DisposeChannel();
        _ = DisposeConnection();
        GC.SuppressFinalize(this);
    }

    private void DisposeChannel()
    {
        try
        {
            lock (_lock)
            {
                _channel?.CloseAsync();
                _channel?.Dispose();
                _channel = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ channel.");
        }
    }
    
    private async Task DisposeConnection()
    {
        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                _connection.CallbackExceptionAsync -= OnCallbackException;
                await _connection.CloseAsync();
                _connection.Dispose();
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection.");
        }
    }
}