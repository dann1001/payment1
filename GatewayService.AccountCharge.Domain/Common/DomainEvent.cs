namespace GatewayService.AccountCharge.Domain.Common;

// Keep domain events free of external dependencies.
// Application layer can adapt these to MediatR notifications later.
public interface IDomainEvent { DateTime OccurredOn { get; } }

public abstract class DomainEventBase : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
