namespace Hiero;
/// <summary>
/// Represents a hook call specification, including the
/// hook's numeric identifier, call data, gas limit, and
/// invocation mode.
/// </summary>
public sealed record HookCall
{
    /// <summary>
    /// The numeric id of the hook to call.
    /// </summary>
    public long HookId { get; private init; }
    /// <summary>
    /// Call data to pass to the hook via the HookContext data field.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; private init; }
    /// <summary>
    /// The gas limit to use for the hook execution.
    /// </summary>
    public ulong GasLimit { get; private init; }
    /// <summary>
    /// Specifies when the hook is invoked relative to the transaction.
    /// </summary>
    public HookCallMode CallMode { get; private init; }
    /// <summary>
    /// Public Constructor, a <code>HookCall</code> is immutable after creation.
    /// </summary>
    /// <param name="hookId">
    /// The numeric id of the hook to call.
    /// </param>
    /// <param name="data">
    /// Call data to pass to the hook.
    /// </param>
    /// <param name="gasLimit">
    /// The gas limit to use for the hook execution.
    /// </param>
    /// <param name="callMode">
    /// Specifies when the hook is invoked relative to the transaction.
    /// Defaults to <see cref="HookCallMode.PreOnly"/>.
    /// </param>
    public HookCall(long hookId, ReadOnlyMemory<byte> data, ulong gasLimit, HookCallMode callMode = HookCallMode.PreOnly)
    {
        HookId = hookId;
        Data = data;
        GasLimit = gasLimit;
        CallMode = callMode;
    }
}
