namespace movieRecommender.Data;

/// <summary>
/// The shared shape every entity holding personal data carries — spec 003-004 §5.1.
/// </summary>
/// <remarks>
/// This interface is the hinge of the whole feature. <see cref="ApplicationDbContext"/>
/// discovers entity types by it and attaches the ownership query filter and the
/// ownerless-write guard automatically, so FR-001, FR-002 and FR-003 hold for an entity
/// purely by virtue of implementing it. That is what NFR-005 asks for: a later story adds
/// a personal entity and writes no authorization code at all.
/// <para>
/// It does <b>not</b> apply to <see cref="Film"/>. A film is shared catalogue data with no
/// owner, which is precisely why only the <i>relationships</i> to it are private.
/// </para>
/// </remarks>
public interface IOwnedRecord
{
    Guid Id { get; }

    /// <summary>The only account that may read or write this record. Set once, never changed.</summary>
    Guid OwnerUserId { get; }

    DateTimeOffset CreatedAt { get; }
}

/// <inheritdoc cref="IOwnedRecord"/>
public abstract class OwnedRecord : IOwnedRecord
{
    protected OwnedRecord()
    {
        // §5.4 "creation data is immutable", as for ApplicationUser: both are set once,
        // at construction. Guid keys have no store-side default to fall back on.
        Id = Guid.CreateVersion7();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Writable so a record can be stamped at creation; immutability afterwards is
    /// enforced in <see cref="ApplicationDbContext.SaveChanges()"/> rather than by the
    /// type, because EF has to be able to set it when materializing a row.
    /// </summary>
    public Guid OwnerUserId { get; set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
