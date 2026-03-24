// SPDX-License-Identifier: Apache-2.0
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex> <contractNum> <methodName> [args...]
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e... 5001 getMessage
//
// Calls a read-only method on an existing smart contract and
// displays the result.

var endpointUrl = args[0];
var nodeNum = long.Parse(args[1]);
var payerNum = long.Parse(args[2]);
var payerKey = Hex.ToBytes(args[3]);
var contractNum = long.Parse(args[4]);
var methodName = args[5];

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, nodeNum),
        new Uri(endpointUrl));
    ctx.Payer = new EntityId(0, 0, payerNum);
    ctx.Signatory = new Signatory(payerKey);
});

var contract = new EntityId(0, 0, contractNum);

// Query the contract (read-only, no state change)
Console.WriteLine($"Querying contract 0.0.{contractNum} method '{methodName}'...");
var result = await client.QueryContractAsync(new QueryContractParams
{
    Contract = contract,
    Gas = 50_000,
    MethodName = methodName
});
Console.WriteLine($"Result: {result}");

// Also get contract info
var info = await client.GetContractInfoAsync(contract);
Console.WriteLine($"Contract account: {info.Account}");
Console.WriteLine($"Balance: {info.Balance} tinybars");
Console.WriteLine($"Memo: {info.Memo}");
