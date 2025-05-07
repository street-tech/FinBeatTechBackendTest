namespace TaskEventListener.Configuration;

/// <summary>
/// Configuration settings for RabbitMQ connection.
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; init; } = 5672;
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? VirtualHost { get; init; } = "/";
}