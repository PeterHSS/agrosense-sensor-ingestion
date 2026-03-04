namespace Api.Domain.Abstractions.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);
}
