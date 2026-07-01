// SPDX-License-Identifier: Apache-2.0
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Proto;

internal static class TransferListExtensions
{
    private static readonly ReadOnlyDictionary<Hiero.EntityId, long> EmptyTransfers = new(new Dictionary<Hiero.EntityId, long>(0));

    internal static ReadOnlyDictionary<Hiero.EntityId, long> ToTransfers(this TransferList list)
    {
        var accountAmounts = list.AccountAmounts;
        var count = accountAmounts.Count;
        if (count == 0)
        {
            return EmptyTransfers;
        }
        var results = new Dictionary<Hiero.EntityId, long>(count);
        foreach (var xfer in accountAmounts)
        {
            var account = xfer.AccountID.AsAddress();
            ref var amount = ref CollectionsMarshal.GetValueRefOrAddDefault(results, account, out _);
            amount += xfer.Amount;
        }
        return new ReadOnlyDictionary<Hiero.EntityId, long>(results);
    }
}
