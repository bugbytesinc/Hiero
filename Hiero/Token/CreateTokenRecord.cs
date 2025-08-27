using Proto;

namespace Hiero;
/// <summary>
/// Record produced from creating a new token.
/// </summary>
public sealed record CreateTokenRecord : TransactionRecord
{
    /// <summary>
    /// The newly created token address.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create token
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Token { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal CreateTokenRecord(Proto.TransactionRecord record) : base(record)
    {
        Token = record.Receipt.TokenID.AsAddress();
    }
}