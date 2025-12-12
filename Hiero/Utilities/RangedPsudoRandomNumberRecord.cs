namespace Hiero;

/// <summary>
/// A transaction record containing information concerning the 
/// newly created ranged pseudo random number.
/// </summary>
public sealed record RangedPseudoRandomNumberRecord : TransactionRecord
{
    /// <summary>
    /// The 32 bit generated number from a ranged PRNG transaction.
    /// </summary>
    public int Number { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal RangedPseudoRandomNumberRecord(Proto.TransactionRecord record) : base(record)
    {
        Number = record.PrngNumber;
    }
}