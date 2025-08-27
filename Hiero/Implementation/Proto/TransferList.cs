using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Proto;

internal static class TransferListExtensions
{
    internal static ReadOnlyDictionary<Hiero.EntityId, long> ToTransfers(this TransferList list)
    {
        var results = new Dictionary<Hiero.EntityId, long>();
        foreach (var xfer in list.AccountAmounts)
        {
            var account = xfer.AccountID.AsAddress();
            results.TryGetValue(account, out long amount);
            results[account] = amount + xfer.Amount;
        }
        return new ReadOnlyDictionary<Hiero.EntityId, long>(results);
    }
}