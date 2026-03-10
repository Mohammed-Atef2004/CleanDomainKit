namespace Domain.SharedKernel;

public abstract class Entity<TId> : IEquatable<Entity<TId>>,IAudiatable,ISoftDeletable
{
    public TId Id { get; protected set; } = default!;

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public DateTime CreatedAt { get;private set; }

    public DateTime? UpdatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
        => obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public bool Equals(Entity<TId>? other)
        => other is not null && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode()
        => Id?.GetHashCode() ?? 0;

    public void Delete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        IsDeleted = true;
    }

    public void SetCreated(string user)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = user;
    }

    public void SetUpdated(string UpdatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        this.UpdatedBy = UpdatedBy;
    }

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
        => a is null ? b is null : a.Equals(b);

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b)
        => !(a == b);
}
