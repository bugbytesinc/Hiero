using System.Diagnostics.CodeAnalysis;

namespace Hiero;
/// <summary>
/// Represents a unique hook identified by its owning
/// entity (account or contract) and a numeric identifier.
/// </summary>
/// <remarks>
/// Modeled similarly to <see cref="Nft"/>, this class
/// consists of an <see cref="EntityId"/> representing
/// the owning account or contract and a numeric identifier
/// chosen by the hook's creator. This class is immutable
/// once created.
/// </remarks>
public sealed record Hook
{
    /// <summary>
    /// The account or contract that owns this hook.
    /// </summary>
    public EntityId Owner { get; private init; }
    /// <summary>
    /// The numeric identifier of the hook within the
    /// owning entity's scope.
    /// </summary>
    public long Id { get; private init; }
    /// <summary>
    /// A sentinel value representing an uninitialized
    /// or non-existent hook.
    /// </summary>
    public static Hook None { get; } = new Hook();
    /// <summary>
    /// Public Constructor, a <code>Hook</code> is immutable after creation.
    /// </summary>
    /// <param name="owner">
    /// The account or contract that owns this hook.
    /// </param>
    /// <param name="id">
    /// The numeric identifier of the hook within the owning entity.
    /// </param>
    public Hook(EntityId owner, long id)
    {
        if (owner is null)
        {
            throw new ArgumentNullException(nameof(owner), "Hook owner is required.");
        }
        if (owner == EntityId.None)
        {
            throw new ArgumentException("Hook owner can not be None.", nameof(owner));
        }
        if (!owner.IsShardRealmNum)
        {
            throw new ArgumentOutOfRangeException(nameof(owner), "Hook owner must be in the form of [shard.realm.num].");
        }
        Owner = owner;
        Id = id;
    }
    /// <summary>
    /// Private constructor for creating a sentinel value
    /// representing an uninitialized hook.
    /// </summary>
    private Hook()
    {
        Owner = EntityId.None;
        Id = 0;
    }
}
internal static class HookExtensions
{
    internal static bool IsNullOrNone([NotNullWhen(false)] this Hook? hook)
    {
        return hook is null || hook == Hook.None;
    }
}
