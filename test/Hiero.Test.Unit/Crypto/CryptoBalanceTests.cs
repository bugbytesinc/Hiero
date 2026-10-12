// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Crypto;

public class CryptoBalanceTests
{
    [Test]
    public async Task Properties_Map_Correctly()
    {
        var balance = (ulong)Generator.Integer(100, 10000);
        var decimals = (uint)Generator.Integer(0, 18);
        var cb = new CryptoBalance { Balance = balance, Decimals = decimals };
        await Assert.That(cb.Balance).IsEqualTo(balance);
        await Assert.That(cb.Decimals).IsEqualTo(decimals);
    }

    [Test]
    public async Task Implicit_Operator_Returns_Balance_Value()
    {
        var balance = (ulong)Generator.Integer(100, 10000);
        var cb = new CryptoBalance { Balance = balance, Decimals = 8 };
        ulong result = cb;
        await Assert.That(result).IsEqualTo(balance);
    }

    [Test]
    public async Task Equivalent_Balances_Are_Equal()
    {
        var balance = (ulong)Generator.Integer(100, 10000);
        var decimals = (uint)Generator.Integer(0, 18);
        var cb1 = new CryptoBalance { Balance = balance, Decimals = decimals };
        var cb2 = new CryptoBalance { Balance = balance, Decimals = decimals };
        await Assert.That(cb1).IsEqualTo(cb2);
        await Assert.That(cb1 == cb2).IsTrue();
    }

    [Test]
    public async Task Different_Balances_Are_Not_Equal()
    {
        var balance = (ulong)Generator.Integer(100, 10000);
        var decimals = (uint)Generator.Integer(0, 18);
        var cb1 = new CryptoBalance { Balance = balance, Decimals = decimals };
        var cb2 = new CryptoBalance { Balance = balance + 1, Decimals = decimals };
        await Assert.That(cb1).IsNotEqualTo(cb2);
        await Assert.That(cb1 == cb2).IsFalse();
    }
}
