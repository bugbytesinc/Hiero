// SPDX-License-Identifier: Apache-2.0
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e...
//
// Creates a fungible token with the payer as treasury,
// mints additional supply, then queries token info.

var endpointUrl = args[0];
var nodeNum = long.Parse(args[1]);
var payerNum = long.Parse(args[2]);
var payerKey = Hex.ToBytes(args[3]);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, nodeNum),
        new Uri(endpointUrl));
    ctx.Payer = new EntityId(0, 0, payerNum);
    ctx.Signatory = new Signatory(payerKey);
});

var treasury = new EntityId(0, 0, payerNum);
var adminEndorsement = new Signatory(payerKey).GetEndorsements().First();

// 1. Create a fungible token
Console.WriteLine("Creating token...");
var createReceipt = await client.CreateTokenAsync(new CreateTokenParams
{
    Name = "Sample Token",
    Symbol = "SMPL",
    Circulation = 1_000_000,
    Decimals = 2,
    Ceiling = 10_000_000,
    Treasury = treasury,
    Administrator = adminEndorsement,
    SupplyEndorsement = adminEndorsement,
    Memo = "Hiero SDK sample token"
});
Console.WriteLine($"Token created: {createReceipt.Token}");

// 2. Mint additional supply
Console.WriteLine("Minting 500,000 additional tokens...");
var mintReceipt = await client.MintTokenAsync(createReceipt.Token, 500_000);
Console.WriteLine($"Mint status: {mintReceipt.Status}");

// 3. Query token info
var info = await client.GetTokenInfoAsync(createReceipt.Token);
Console.WriteLine($"Token: {info.Name} ({info.Symbol})");
Console.WriteLine($"Circulation: {info.Circulation}");
Console.WriteLine($"Decimals: {info.Decimals}");
Console.WriteLine($"Treasury: {info.Treasury}");
