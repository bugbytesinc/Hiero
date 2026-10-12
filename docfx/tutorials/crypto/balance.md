---
title: Get Account Balance
---

# Get Account Balance

Account balances are read from a mirror node. Create a [`MirrorRestClient`](xref:Hiero.MirrorRestClient) around an `HttpClient` pointed at the mirror node's REST endpoint; no account, key or fee is needed. The [`GetAccountBalanceAsync`](xref:Hiero.Mirror.AccountDataExtensions.GetAccountBalanceAsync*) method returns the balance of an account — or of a contract, since contracts are accounts on the mirror node — in [_tinybars_](https://help.hedera.com/hc/en-us/articles/360000674317-What-are-the-official-HBAR-cryptocurrency-denominations-), or `null` if the mirror node does not know the account. The following code example illustrates a small program performing these actions:

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://testnet.mirrornode.hedera.com 0.0.98
        var mirrorUrl = args[0];
        if (!EntityId.TryParseShardRealmNum(args[1], out var queryAccount))
            throw new ArgumentException($"Invalid query account: {args[1]}");

        try
        {
            var mirror = new MirrorRestClient(new HttpClient { BaseAddress = new Uri(mirrorUrl) });
            var balance = await mirror.GetAccountBalanceAsync(queryAccount);
            Console.WriteLine(balance is null
                ? $"Account {queryAccount} was not found."
                : $"Account Balance for {queryAccount} is {balance:#,#} tinybars.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```

## Things to know

- **The mirror node trails consensus by a few seconds.** To read a balance that reflects a transaction you just submitted, wait until the mirror node's latest consensus timestamp ([`GetLatestConsensusTimestampAsync`](xref:Hiero.Mirror.TransactionTimestampDataExtensions.GetLatestConsensusTimestampAsync*)) reaches the transaction's consensus timestamp.
- **Historical balances:** pass a [`TimestampFilter`](xref:Hiero.Mirror.Filters.TimestampFilter) to read the balance as of an earlier consensus instant.
- **Token balances:** use [`GetAccountTokenBalanceAsync`](xref:Hiero.Mirror.TokenBalanceDataExtensions.GetAccountTokenBalanceAsync*) for one token, or [`GetAccountTokenHoldingsAsync`](xref:Hiero.Mirror.TokenHoldingDataExtensions.GetAccountTokenHoldingsAsync*) for all of an account's holdings.
- **Reading from a consensus node instead:** the free consensus-node balance query was removed from the public Hedera networks in July 2026 (nodes answer it with `BUSY`), and this SDK no longer offers it. When a balance must be current as of consensus rather than the mirror node, [`GetAccountInfoAsync`](xref:Hiero.AccountInfoExtensions.GetAccountInfoAsync*) returns it in `AccountInfo.Balance`, as a paid query that needs a `Payer` and `Signatory`.
