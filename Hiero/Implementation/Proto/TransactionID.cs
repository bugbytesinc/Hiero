using Hiero;
using System;

namespace Proto;

public sealed partial class TransactionID
{
    internal TransactionID(TransactionId transaction) : this()
    {
        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction), "Transaction is missing. Please check that it is not null.");
        }
        AccountID = new AccountID(transaction.Payer);
        TransactionValidStart = new Timestamp
        {
            Seconds = transaction.ValidStartSeconds,
            Nanos = transaction.ValidStartNanos
        };
        Scheduled = transaction.Scheduled;
        Nonce = transaction.ChildNonce;
    }
}

internal static class TransactionIDExtensions
{
    internal static TransactionId AsTxId(this TransactionID? id)
    {
        if (id is not null)
        {
            var timestamp = id.TransactionValidStart;
            if (timestamp is not null)
            {
                return new TransactionId(id.AccountID.AsAddress(), timestamp.Seconds, timestamp.Nanos, id.Scheduled, id.Nonce);
            }
        }
        return TransactionId.None;
    }
}