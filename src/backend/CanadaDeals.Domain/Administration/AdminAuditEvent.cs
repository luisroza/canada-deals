namespace CanadaDeals.Domain.Administration;

public sealed class AdminAuditEvent
{
    public const int MaxSummaryLength = 500;

    private AdminAuditEvent() { }

    public Guid Id { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static AdminAuditEvent Create(
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string summary,
        DateTimeOffset createdAt)
    {
        if (actorUserId == Guid.Empty) throw new ArgumentException("Actor user is required.", nameof(actorUserId));
        if (entityId == Guid.Empty) throw new ArgumentException("Entity is required.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(action) || action.Length > 80) throw new ArgumentException("Action is required and limited to 80 characters.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType) || entityType.Length > 80) throw new ArgumentException("Entity type is required and limited to 80 characters.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > MaxSummaryLength) throw new ArgumentException($"Summary is required and limited to {MaxSummaryLength} characters.", nameof(summary));

        return new AdminAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            Summary = summary.Trim(),
            CreatedAt = createdAt
        };
    }
}
