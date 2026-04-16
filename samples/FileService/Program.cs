// SPDX-License-Identifier: Apache-2.0
using System.Text;
using Hiero;

// Usage: dotnet run -- <endpointUrl> <nodeNum> <payerNum> <payerKeyHex>
// Example: dotnet run -- https://2.testnet.hedera.com:50211 5 1001 302e...
//
// Creates a file on the network, reads it back, appends content,
// and reads it again.

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

var payerEndorsement = new Signatory(payerKey).GetEndorsements().First();

// 1. Create a file
Console.WriteLine("Creating file...");
#region CreateFile
var createReceipt = await client.CreateFileAsync(new CreateFileParams
{
    Contents = Encoding.UTF8.GetBytes("Hello from Hiero SDK!"),
    Endorsements = new[] { payerEndorsement },
    Memo = "Sample file"
});
Console.WriteLine($"File created: {createReceipt.File}");
#endregion

// 2. Read file contents
var contents = await client.GetFileContentAsync(createReceipt.File);
Console.WriteLine($"File contents: {Encoding.UTF8.GetString(contents.Span)}");

// 3. Append to file
Console.WriteLine("Appending to file...");
#region AppendFile
await client.AppendFileAsync(new AppendFileParams
{
    File = createReceipt.File,
    Contents = Encoding.UTF8.GetBytes(" More content appended.")
});
#endregion

// 4. Read updated contents
var updated = await client.GetFileContentAsync(createReceipt.File);
Console.WriteLine($"Updated contents: {Encoding.UTF8.GetString(updated.Span)}");

// 5. Query file info
var info = await client.GetFileInfoAsync(createReceipt.File);
Console.WriteLine($"File memo: {info.Memo}");
Console.WriteLine($"File size: {info.Size} bytes");
Console.WriteLine($"Expiration: {info.Expiration}");
