// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Crypto;

public class CryptoAllowanceTests
{
    [Test]
    public async Task Constructor_Maps_Properties()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var allowance = new CryptoAllowance(owner, spender, amount);
        await Assert.That(allowance.Owner).IsEqualTo(owner);
        await Assert.That(allowance.Spender).IsEqualTo(spender);
        await Assert.That(allowance.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task Null_Owner_Throws_ArgumentException()
    {
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new CryptoAllowance(null, spender, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Owner_Throws_ArgumentException()
    {
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new CryptoAllowance(EntityId.None, spender, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Null_Spender_Throws_ArgumentException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new CryptoAllowance(owner, null, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Spender_Throws_ArgumentException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new CryptoAllowance(owner, EntityId.None, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Negative_Amount_Throws_ArgumentOutOfRangeException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { new CryptoAllowance(owner, spender, -1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Zero_Amount_Is_Valid()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        var allowance = new CryptoAllowance(owner, spender, 0);
        var expectedAmount = 0L;
        await Assert.That(allowance.Amount).IsEqualTo(expectedAmount);
    }

    [Test]
    public async Task Equivalent_Allowances_Are_Equal()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var a1 = new CryptoAllowance(owner, spender, amount);
        var a2 = new CryptoAllowance(owner, spender, amount);
        await Assert.That(a1).IsEqualTo(a2);
        await Assert.That(a1 == a2).IsTrue();
    }

    [Test]
    public async Task Different_Allowances_Are_Not_Equal()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var a1 = new CryptoAllowance(owner, spender, amount);
        var a2 = new CryptoAllowance(owner, spender, amount + 1);
        await Assert.That(a1).IsNotEqualTo(a2);
        await Assert.That(a1 == a2).IsFalse();
    }
}
