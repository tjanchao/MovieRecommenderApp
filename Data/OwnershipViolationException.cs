namespace movieRecommender.Data;

/// <summary>
/// A write that would have produced an ownerless record, re-parented an existing one, or
/// written a record belonging to somebody other than the acting user.
/// </summary>
/// <remarks>
/// A programming error, not a user-facing condition — every legitimate path stamps the
/// owner. It is deliberately not caught and rendered anywhere.
/// <para>
/// FR-010: the message names the entity type and the owner ids involved, never the
/// contents of the record. An exception message reaches a log, and a log is the easiest
/// place to leak the thing this whole feature exists to protect.
/// </para>
/// </remarks>
public sealed class OwnershipViolationException : InvalidOperationException
{
    public OwnershipViolationException(string message) : base(message)
    {
    }
}
