using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex> <tokenNum> <recipientNum>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e... 5001 1002
//
// Airdrops fungible tokens from the payer to a recipient.
// If the recipient is not associated with the token and has no
// auto-association slots, a pending airdrop is created.

var endpointUrl = args[0];
var nodeNum = long.Parse(args[1]);
var payerNum = long.Parse(args[2]);
var payerKey = Hex.ToBytes(args[3]);
var tokenNum = long.Parse(args[4]);
var recipientNum = long.Parse(args[5]);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, nodeNum),
        new Uri(endpointUrl));
    ctx.Payer = new EntityId(0, 0, payerNum);
    ctx.Signatory = new Signatory(payerKey);
});

var token = new EntityId(0, 0, tokenNum);
var from = new EntityId(0, 0, payerNum);
var to = new EntityId(0, 0, recipientNum);

// Airdrop 100 tokens (smallest unit) to recipient
Console.WriteLine($"Airdropping tokens from 0.0.{payerNum} to 0.0.{recipientNum}...");
var receipt = await client.AirdropTokenAsync(token, from, to, 100);
Console.WriteLine($"Airdrop status: {receipt.Status}");
