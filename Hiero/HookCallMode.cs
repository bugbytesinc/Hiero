namespace Hiero;

/// <summary>
/// Specifies when a hook is invoked relative to the transaction.
/// </summary>
public enum HookCallMode
{
    /// <summary>
    /// The hook is called once before the transaction, invoking
    /// a method with logical signature <c>allow(HookContext, ProposedTransfers)</c>.
    /// </summary>
    PreOnly,
    /// <summary>
    /// The hook is called twice: once before the transaction invoking
    /// <c>allowPre(HookContext, ProposedTransfers)</c>, and once after
    /// invoking <c>allowPost(HookContext, ProposedTransfers)</c>.
    /// </summary>
    PreAndPost
}
