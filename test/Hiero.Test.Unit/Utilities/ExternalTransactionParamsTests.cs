// SPDX-License-Identifier: Apache-2.0

namespace Hiero.Test.Unit.Utilities;

public class ExternalTransactionParamsTests
{
    [Test]
    public async Task GetNetworkParams_Throws_InvalidOperationException_With_Descriptive_Message()
    {
        // Regression guard for L12. ExternalTransactionParams inherits
        // TransactionParams<TransactionReceipt> so C# method resolution
        // matches client.ExecuteAsync(externalParams) against the generic
        // ConsensusClient.ExecuteAsync<T>(TransactionParams<T>) instance
        // method, which calls GetNetworkParams() and assumes the result
        // implements INetworkParams<TReceipt>. ExternalTransactionParams
        // does not — its orchestrator wraps it. Before L12 this produced
        // an InvalidCastException from the base-class default cast. Now
        // the override throws a descriptive InvalidOperationException
        // naming the correct extensions.
        var externalParams = new ExternalTransactionParams
        {
            SignedTransactionBytes = new byte[] { 0x01, 0x02, 0x03 }
        };
        var baseParams = (TransactionParams<TransactionReceipt>)externalParams;
        var ex = Assert.Throws<InvalidOperationException>(() => baseParams.GetNetworkParams());
        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.Message).Contains("ExternalTransactionParams");
        await Assert.That(ex.Message).Contains("ExecuteExternalTransactionAsync");
        await Assert.That(ex.Message).Contains("SubmitExternalTransactionAsync");
    }
}
