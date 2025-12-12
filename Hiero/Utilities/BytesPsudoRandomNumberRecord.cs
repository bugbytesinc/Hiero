namespace Hiero;
/// <summary>
/// A transaction record containing information concerning the 
/// newly created unbounded 384 bit pseudo random number.
/// </summary>
public sealed record BytesPseudoRandomNumberRecord : TransactionRecord
{
    /// <summary>
    /// The unbounded 384 bit pseudo random number.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal BytesPseudoRandomNumberRecord(Proto.TransactionRecord record) : base(record)
    {
        Bytes = record.PrngBytes.Memory;
    }
}