// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Hiero.Mirror;

// Usage: dotnet run -- <mirrorRestUrl> <queryAccountNum>
// Example: dotnet run -- https://testnet.mirrornode.hedera.com 98
//
// Balances are read from a mirror node: free, and no account is needed.
// (Consensus nodes no longer answer balance queries; the public networks
// retired that query in July 2026.)

var mirrorRestUrl = args[0];
var queryAccountNum = long.Parse(args[1]);

var mirror = new MirrorRestClient(new HttpClient { BaseAddress = new Uri(mirrorRestUrl) });

var account = new EntityId(0, 0, queryAccountNum);
var balance = await mirror.GetAccountBalanceAsync(account);
Console.WriteLine(balance is long tinybars
    ? $"Account 0.0.{queryAccountNum} balance: {tinybars:#,#} tinybars ({tinybars / 100_000_000m} hbar)"
    : $"Account 0.0.{queryAccountNum} was not found.");
