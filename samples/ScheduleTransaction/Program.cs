// SPDX-License-Identifier: Apache-2.0
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex> <recipientNum> <amount>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e... 1002 50000000
//
// Schedules a crypto transfer for deferred execution.
// The transfer will execute once all required signatures are collected.

var endpointUrl = args[0];
var nodeNum = long.Parse(args[1]);
var payerNum = long.Parse(args[2]);
var payerKey = Hex.ToBytes(args[3]);
var recipientNum = long.Parse(args[4]);
var amount = long.Parse(args[5]);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, nodeNum),
        new Uri(endpointUrl));
    ctx.Payer = new EntityId(0, 0, payerNum);
    ctx.Signatory = new Signatory(payerKey);
});

var sender = new EntityId(0, 0, payerNum);
var recipient = new EntityId(0, 0, recipientNum);

// Schedule a transfer for deferred execution
Console.WriteLine("Scheduling transfer...");
#region ScheduleTransfer
var receipt = await client.ScheduleAsync(new ScheduleParams
{
    Transaction = new TransferParams
    {
        CryptoTransfers = new[]
        {
            new CryptoTransfer(sender, -amount),
            new CryptoTransfer(recipient, amount)
        }
    },
    Memo = "Scheduled transfer from Hiero SDK sample",
    Expiration = new ConsensusTimeStamp(DateTime.UtcNow.AddHours(1))
});
Console.WriteLine($"Schedule created: {receipt.Schedule}");
#endregion

// Query the schedule
var info = await client.GetScheduleInfoAsync(receipt.Schedule);
Console.WriteLine($"Schedule memo: {info.Memo}");
Console.WriteLine($"Executed: {info.Executed?.ToString() ?? "pending"}");
Console.WriteLine($"Expiration: {info.Expiration}");
