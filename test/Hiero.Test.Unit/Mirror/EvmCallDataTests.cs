// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class EvmCallDataTests
{
    [Test]
    public async Task Default_Constructor_Has_Null_Properties()
    {
        var data = new EvmCallData();
        await Assert.That(data.Block).IsNull();
        await Assert.That(data.Data).IsNull();
        await Assert.That(data.From).IsNull();
        await Assert.That(data.Gas).IsNull();
        await Assert.That(data.GasPrice).IsNull();
        await Assert.That(data.To).IsNull();
        await Assert.That(data.Value).IsNull();
        await Assert.That(data.EstimateGas).IsFalse();
    }

    [Test]
    public async Task Convenience_Constructor_Sets_To_And_Data()
    {
        var contractAddress = new EvmAddress(new byte[20]);
        var method = "transfer(address,uint256)";
        var data = new EvmCallData(contractAddress, method);
        await Assert.That(data.To).IsEqualTo(contractAddress);
        await Assert.That(data.Data).IsNotNull();
        await Assert.That(data.Data!.Value.Length).IsGreaterThan(0);
    }
}
