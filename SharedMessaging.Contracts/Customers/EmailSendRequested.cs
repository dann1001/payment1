namespace SharedMessaging.Contracts.Customers;

public sealed record EmailSendRequested(
    Guid ToCustomerId,
    string? ToEmail,                 // <-- NEW (nullable for backward compatibility)
    string TemplateName,
    object TemplateModel,
    string? CorrelationId = null
);
