// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;
/// <summary>
/// A transaction record containing information concerning the newly created account.
/// </summary>
public sealed record CreateAccountRecord : TransactionRecord
{
    /// <summary>
    /// The Native Hedera address of the newly created account.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create account
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Address { get; internal init; }
    /// <summary>
    /// The new EVM address of the account created
    /// by this transaction.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the record carries no
    /// 20-byte EVM address, which happens when the network assigned none
    /// (for example, to an account with an Ed25519 key) or when the create
    /// account method was scheduled as a pending transaction.  Use
    /// <see cref="Address"/> in that case.
    /// </remarks>
    public EvmAddress EvmAddress { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal CreateAccountRecord(Proto.TransactionRecord record) : base(record)
    {
        Address = record.Receipt.AccountID.AsAddress();
        EvmAddress = record.EvmAddress is { Length: 20 } ? new EvmAddress(record.EvmAddress.Memory) : EvmAddress.None;
    }
}