using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeAccountNum> <queryAccountNum>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 98

var endpointUrl = args[0];
var nodeAccountNum = long.Parse(args[1]);
var queryAccountNum = long.Parse(args[2]);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, nodeAccountNum),
        new Uri(endpointUrl));
});

var account = new EntityId(0, 0, queryAccountNum);
var balance = await client.GetAccountBalanceAsync(account);
Console.WriteLine($"Account 0.0.{queryAccountNum} balance: {balance:#,#} tinybars ({balance / 100_000_000m} hbar)");
