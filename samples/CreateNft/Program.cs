// SPDX-License-Identifier: Apache-2.0
using System.Text;
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e...
//
// Creates an NFT collection, mints three NFTs, and queries info.

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

// 1. Create NFT collection
Console.WriteLine("Creating NFT collection...");
#region CreateNft
var createReceipt = await client.CreateNftAsync(new CreateNftParams
{
    Name = "Sample NFTs",
    Symbol = "SNFT",
    Ceiling = 100,
    Treasury = treasury,
    Administrator = adminEndorsement,
    SupplyEndorsement = adminEndorsement,
    Memo = "Hiero SDK sample NFT collection"
});
Console.WriteLine($"NFT collection created: {createReceipt.Token}");
#endregion

// 2. Mint NFTs with metadata
Console.WriteLine("Minting 3 NFTs...");
#region MintNftBatch
var mintReceipt = await client.MintNftsAsync(new MintNftParams
{
    Token = createReceipt.Token,
    Metadata = new[]
    {
        (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{\"name\":\"NFT #1\"}"),
        (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{\"name\":\"NFT #2\"}"),
        (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{\"name\":\"NFT #3\"}")
    }
});
Console.WriteLine($"Minted serial numbers: {string.Join(", ", mintReceipt.SerialNumbers)}");
#endregion

// 3. Query NFT info
foreach (var serial in mintReceipt.SerialNumbers)
{
    var info = await client.GetNftInfoAsync(new Nft(createReceipt.Token, serial));
    Console.WriteLine($"NFT #{serial} - Owner: {info.Owner}, Metadata: {Encoding.UTF8.GetString(info.Metadata.Span)}");
}
