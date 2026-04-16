// SPDX-License-Identifier: Apache-2.0
using System.Text;
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e...
//
// Creates an HCS topic, submits a message, and queries topic info.

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

// 1. Create a topic
Console.WriteLine("Creating HCS topic...");
#region CreateTopic
var createReceipt = await client.CreateTopicAsync(new CreateTopicParams
{
    Memo = "Hiero SDK Sample Topic",
    RenewPeriod = TimeSpan.FromDays(90),
    RenewAccount = new EntityId(0, 0, payerNum)
});
Console.WriteLine($"Topic created: {createReceipt.Topic}");
#endregion

// 2. Submit a message
Console.WriteLine("Submitting message...");
#region SubmitMessage
var message = Encoding.UTF8.GetBytes("Hello from Hiero SDK!");
var submitReceipt = await client.SubmitMessageAsync(createReceipt.Topic, message);
Console.WriteLine($"Message submitted - Sequence: {submitReceipt.SequenceNumber}");
Console.WriteLine($"Status: {submitReceipt.Status}");
#endregion

// 3. Query topic info
var info = await client.GetTopicInfoAsync(createReceipt.Topic);
Console.WriteLine($"Topic memo: {info.Memo}");
Console.WriteLine($"Sequence number: {info.SequenceNumber}");
Console.WriteLine($"Expiration: {info.Expiration}");
