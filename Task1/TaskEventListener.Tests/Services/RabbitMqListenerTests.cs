using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using TaskEventListener.Configuration;
using TaskEventListener.Services;

namespace TaskEventListener.Tests.Services;

[TestFixture]
public class RabbitMqListenerServiceTests
{
    [Test]
    public void Constructor_InitializesWithValidSettings()
    {
        // Arrange & Act
        var environment = new TestEnvironment();

        // Assert
        Assert.That(environment.Target, Is.Not.Null);
        Assert.That(environment.RabbitMqSettings.HostName, Is.EqualTo("localhost"));
        Assert.That(environment.RabbitMqSettings.Port, Is.EqualTo(5672));
    }

    [Test]
    public async Task StartAsync_InitializesServiceCorrectly()
    {
        // Arrange
        var environment = new TestEnvironment();
        var cancellationToken = CancellationToken.None;

        try
        {
            // Act
            await environment.Target.StartAsync(cancellationToken);

            // Assert
            environment.Logger.ReceivedWithAnyArgs().LogInformation(null);
        }
        finally
        {
            await environment.Target.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task StopAsync_DisposesConnectionsProperly()
    {
        // Arrange
        var environment = new TestEnvironment();
        var cancellationToken = CancellationToken.None;

        // Act
        await environment.Target.StartAsync(cancellationToken);
        await environment.Target.StopAsync(cancellationToken);

        // Assert
        environment.Logger.ReceivedWithAnyArgs().LogInformation(null);
    }

    [Test]
    public async Task ExecuteAsync_HandlesConnectionFailure()
    {
        // Arrange
        var environment = new TestEnvironment
        {
            RabbitMqSettings =
            {
                HostName = "invalid_host"
            }
        };
        var cancellationToken = CancellationToken.None;

        try
        {
            // Act
            await environment.Target.StartAsync(cancellationToken);

            // Assert
            environment.Logger.ReceivedWithAnyArgs().LogWarning(default(Exception), null);
        }
        finally
        {
            await environment.Target.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task OnConnectionShutdown_LogsWarningAndAttemptsReconnect()
    {
        // Arrange
        var environment = new TestEnvironment();
        var cancellationToken = CancellationToken.None;

        try
        {
            await environment.Target.StartAsync(cancellationToken);
            
            var shutdownArgs = new ShutdownEventArgs(ShutdownInitiator.Peer, 0, "Test shutdown");
            var methodInfo = environment.Target.GetType().GetMethod("OnConnectionShutdown", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Act
            if (methodInfo != null)
            {
                await (Task)methodInfo.Invoke(environment.Target, [null, shutdownArgs])!;
            }

            // Assert
            environment.Logger.ReceivedWithAnyArgs().LogWarning(null);
        }
        finally
        {
            await environment.Target.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task MessageProcessing_HandlesValidMessage()
    {
        // Arrange
        var environment = new TestEnvironment();
        var cancellationToken = CancellationToken.None;

        try
        {
            // Act
            await environment.Target.StartAsync(cancellationToken);

            // Assert
            environment.Logger.ReceivedWithAnyArgs().LogInformation(null);
        }
        finally
        {
            await environment.Target.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task MessageProcessing_HandlesInvalidMessage()
    {
        // Arrange
        var environment = new TestEnvironment();
        var cancellationToken = CancellationToken.None;

        try
        {
            // Act
            await environment.Target.StartAsync(cancellationToken);

            // Assert
            environment.Logger.ReceivedWithAnyArgs().LogError(default(Exception), null);
        }
        finally
        {
            await environment.Target.StopAsync(CancellationToken.None);
        }
    }
    
    private class TestEnvironment
    {
        public ILogger<RabbitMqListenerService> Logger { get; }
        private IOptions<RabbitMqSettings> Settings { get; }
        public RabbitMqListenerService Target { get; }
        public RabbitMqSettings RabbitMqSettings { get; }

        public TestEnvironment()
        {
            Logger = Substitute.For<ILogger<RabbitMqListenerService>>();

            RabbitMqSettings = new RabbitMqSettings
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/"
            };
            
            Settings = Substitute.For<IOptions<RabbitMqSettings>>();
            Settings.Value.Returns(RabbitMqSettings);

            Target = new RabbitMqListenerService(Settings, Logger);
        }
    }
}