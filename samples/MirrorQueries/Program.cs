// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Hiero.Mirror;

// Usage: dotnet run -- <mirrorBaseUrl> <accountNum>
// Example: dotnet run -- https://testnet.mirrornode.hedera.com 98
//
// Queries the Mirror Node REST API for account information.
// No payer account or private keys required.

var mirrorBaseUrl = args[0];
var accountNum = long.Parse(args[1]);

var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri(mirrorBaseUrl)
});

var account = new EntityId(0, 0, accountNum);

Console.WriteLine($"Querying mirror node for account 0.0.{accountNum}...");
Console.WriteLine($"Mirror endpoint: {mirror.EndpointUrl}");

// Query account data from the mirror node
var accountData = await mirror.GetAccountAsync(account);
if (accountData is not null)
{
    Console.WriteLine($"Account: {accountData.Account}");
    Console.WriteLine($"Balance: {accountData.Balances.Balance} tinybars");
    Console.WriteLine($"Endorsement: {accountData.Endorsement}");
    Console.WriteLine($"Memo: {accountData.Memo}");
    Console.WriteLine($"Auto-renew period: {accountData.AutoRenewPeriod}s");
    Console.WriteLine($"EVM address: {accountData.EvmAddress}");
}
else
{
    Console.WriteLine("Account not found.");
}
