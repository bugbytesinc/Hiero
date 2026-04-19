// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Token;

public class TokenAllowanceTests
{
    [Test]
    public async Task Constructor_Maps_Properties()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        long amount = Generator.Integer(500, 1000);
        var allowance = new TokenAllowance(token, owner, spender, amount);
        await Assert.That(allowance.Token).IsEqualTo(token);
        await Assert.That(allowance.Owner).IsEqualTo(owner);
        await Assert.That(allowance.Spender).IsEqualTo(spender);
        await Assert.That(allowance.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task Null_Token_Throws_ArgumentException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new TokenAllowance(null, owner, spender, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Token_Throws_ArgumentException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new TokenAllowance(EntityId.None, owner, spender, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Null_Owner_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new TokenAllowance(token, null, spender, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Owner_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new TokenAllowance(token, EntityId.None, spender, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Null_Spender_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new TokenAllowance(token, owner, null, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Spender_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var ex = Assert.Throws<ArgumentException>(() => { new TokenAllowance(token, owner, EntityId.None, amount); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Negative_Amount_Throws_ArgumentOutOfRangeException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { new TokenAllowance(token, owner, spender, -1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Zero_Amount_Is_Valid()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var allowance = new TokenAllowance(token, owner, spender, 0);
        var expectedAmount = 0L;
        await Assert.That(allowance.Amount).IsEqualTo(expectedAmount);
    }

    [Test]
    public async Task Equivalent_Allowances_Are_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        long amount = Generator.Integer(500, 1000);
        var a1 = new TokenAllowance(token, owner, spender, amount);
        var a2 = new TokenAllowance(token, owner, spender, amount);
        await Assert.That(a1).IsEqualTo(a2);
        await Assert.That(a1 == a2).IsTrue();
    }
}
