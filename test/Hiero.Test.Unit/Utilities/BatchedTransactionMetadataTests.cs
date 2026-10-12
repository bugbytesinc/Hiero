// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Utilities;

public class BatchedTransactionMetadataTests
{
    [Test]
    public async Task Submitting_Metadata_On_Its_Own_Explains_What_To_Do()
    {
        await using var client = new ConsensusClient(ctx =>
        {
            ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("http://localhost:50211"));
            ctx.Payer = new EntityId(0, 0, 2);
        });
        var metadata = new BatchedTransactionMetadata { TransactionParams = new TransferParams() };
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await client.SubmitAsync(metadata));
        await Assert.That(exception!.Message).Contains("BatchedTransactionParams");
    }
}
