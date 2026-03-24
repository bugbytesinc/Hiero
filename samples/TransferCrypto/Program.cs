// SPDX-License-Identifier: Apache-2.0
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex> <fromNum> <fromKeyHex> <toNum> <amount>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e... 1001 302e... 1002 100000000

var endpointUrl = args[0];
var nodeNum = long.Parse(args[1]);
var payerNum = long.Parse(args[2]);
var payerKey = Hex.ToBytes(args[3]);
var fromNum = long.Parse(args[4]);
var fromKey = Hex.ToBytes(args[5]);
var toNum = long.Parse(args[6]);
var amount = long.Parse(args[7]);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, nodeNum),
        new Uri(endpointUrl));
    ctx.Payer = new EntityId(0, 0, payerNum);
    ctx.Signatory = new Signatory(payerKey, new Signatory(fromKey));
});

var from = new EntityId(0, 0, fromNum);
var to = new EntityId(0, 0, toNum);

var receipt = await client.TransferAsync(from, to, amount);
Console.WriteLine($"Transfer status: {receipt.Status}");
Console.WriteLine($"Transferred {amount} tinybars from 0.0.{fromNum} to 0.0.{toNum}");
