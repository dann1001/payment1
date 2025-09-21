namespace SharedMessaging.BuildingBlocks.Messaging;

public interface IIntegrationEventHandler<T>
{
    Task Handle(T message, MessageContext context, CancellationToken cancellationToken);
}
