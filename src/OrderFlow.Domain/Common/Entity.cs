namespace OrderFlow.Domain.Common;

public abstract class Entity
{
    public int Id { get; protected set; }
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
}