namespace TaskManager.Application.Interfaces;

/// <summary>
/// Defines the contract for sending messages to a message broker.
/// </summary>
public interface IMessageProducer
{
    /// <summary>
    /// Sends a message asynchronously.
    /// </summary>
    /// <param name="exchangeName">The target exchange name.</param>
    /// <param name="routingKey">The routing key for the message.</param>
    /// <param name="message">The message body (typically serialized).</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task SendMessageAsync(string exchangeName, string routingKey, string message);
}