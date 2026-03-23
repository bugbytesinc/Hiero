namespace Hiero;
/// <summary>
/// Represents the metadata required to create a new hook
/// on an account or contract.
/// </summary>
/// <remarks>
/// This is a simplified representation that flattens the
/// underlying protobuf structure. The hook extension point
/// (currently only <c>ACCOUNT_ALLOWANCE_HOOK</c>) and the
/// EVM-specific nesting are handled internally during
/// proto projection.
/// </remarks>
public sealed record HookMetadata
{
    /// <summary>
    /// An arbitrary numeric identifier for the hook,
    /// chosen by the creator.
    /// </summary>
    public long Id { get; private init; }
    /// <summary>
    /// The contract that implements the hook's EVM bytecode.
    /// </summary>
    public EntityId Contract { get; private init; }
    /// <summary>
    /// Optional admin key that can be used to remove or
    /// replace the hook, or perform transactions that
    /// customize the hook.
    /// </summary>
    public Endorsement? AdminKey { get; private init; }
    /// <summary>
    /// Optional initial storage values for the hook.
    /// </summary>
    public IEnumerable<HookStorageEntry>? InitialStorage { get; private init; }
    /// <summary>
    /// Public Constructor, a <code>HookMetadata</code> is immutable after creation.
    /// </summary>
    /// <param name="id">
    /// An arbitrary numeric identifier for the hook.
    /// </param>
    /// <param name="contract">
    /// The contract that implements the hook's EVM bytecode.
    /// </param>
    /// <param name="adminKey">
    /// Optional admin key for the hook.
    /// </param>
    /// <param name="initialStorage">
    /// Optional initial storage values for the hook.
    /// </param>
    public HookMetadata(long id, EntityId contract, Endorsement? adminKey = null, IEnumerable<HookStorageEntry>? initialStorage = null)
    {
        if (contract.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(contract), "Hook contract is required. Please check that it is not null or None.");
        }
        Id = id;
        Contract = contract;
        AdminKey = adminKey;
        InitialStorage = initialStorage;
    }
}
