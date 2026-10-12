// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
using System.Numerics;

namespace Hiero.Test.Unit.Contract;

public class EvmTransactionInputTests
{
    [Test]
    public async Task Properties_Have_Default_Values()
    {
        var input = new EvmTransactionInput();
        await Assert.That(input.EvmNonce).IsEqualTo(0L);
        await Assert.That(input.GasPrice).IsEqualTo(0L);
        await Assert.That(input.GasLimit).IsEqualTo(0L);
        await Assert.That(input.ChainId).IsEqualTo(BigInteger.Zero);
        await Assert.That(input.ValueInTinybars).IsEqualTo(0L);
        await Assert.That(input.MethodName).IsNull();
        await Assert.That(input.MethodParameters).IsNull();
    }

    [Test]
    public async Task RlpEncode_Null_Signatory_Throws()
    {
        var input = new EvmTransactionInput
        {
            EvmNonce = 1,
            GasPrice = 1,
            GasLimit = 21000,
            ToEvmAddress = new EvmAddress(new byte[20]),
            ChainId = BigInteger.One
        };
        var ex = Assert.Throws<ArgumentNullException>(() => { input.RlpEncode(null); });
        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ParamName).IsEqualTo("signatory");
    }

    [Test]
    public async Task RlpEncode_Parameters_Without_MethodName_Throws()
    {
        var input = new EvmTransactionInput
        {
            EvmNonce = 1,
            GasPrice = 1,
            GasLimit = 21000,
            ToEvmAddress = new EvmAddress(new byte[20]),
            ChainId = BigInteger.One,
            MethodParameters = new object[] { 42 }
            // MethodName intentionally left null
        };
        var ex = Assert.Throws<ArgumentException>(() => { input.RlpEncode(null); });
        await Assert.That(ex).IsNotNull();
    }
}
