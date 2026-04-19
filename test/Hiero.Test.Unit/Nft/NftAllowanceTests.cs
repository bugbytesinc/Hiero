// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8625 // Null assignments and conversions are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Nft;

public class NftAllowanceTests
{
    [Test]
    public async Task Constructor_With_Token_Maps_Properties()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var serials = new long[] { 1, 2, 3 };
        var allowance = new NftAllowance(token, owner, spender, serials);
        await Assert.That(allowance.Token).IsEqualTo(token);
        await Assert.That(allowance.Owner).IsEqualTo(owner);
        await Assert.That(allowance.Spender).IsEqualTo(spender);
        await Assert.That(allowance.SerialNumbers).IsNotNull();
        await Assert.That(allowance.SerialNumbers!.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Constructor_With_Nft_Asset_Maps_Properties()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        long serial = Generator.Integer(1, 100);
        var nft = new Hiero.Nft(token, serial);
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var allowance = new NftAllowance(nft, owner, spender);
        await Assert.That(allowance.Token).IsEqualTo(token);
        await Assert.That(allowance.Owner).IsEqualTo(owner);
        await Assert.That(allowance.Spender).IsEqualTo(spender);
        await Assert.That(allowance.SerialNumbers).IsNotNull();
        await Assert.That(allowance.SerialNumbers!.Count).IsEqualTo(1);
        await Assert.That(allowance.SerialNumbers[0]).IsEqualTo(serial);
    }

    [Test]
    public async Task Null_Token_Throws_ArgumentException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance((EntityId)null, owner, spender); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Token_Throws_ArgumentException()
    {
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance(EntityId.None, owner, spender); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Null_Owner_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance(token, null, spender); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Owner_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance(token, EntityId.None, spender); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Null_Spender_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance(token, owner, null); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Spender_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance(token, owner, EntityId.None); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task DelegatingSpender_Without_SerialNumbers_Throws_ArgumentException()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var delegatingSpender = new EntityId(0, 0, Generator.Integer(600, 800));
        var ex = Assert.Throws<ArgumentException>(() => { new NftAllowance(token, owner, spender, serialNumbers: null, delegatingSpender: delegatingSpender); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task With_SerialNumbers_And_Delegate_Works()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var delegatingSpender = new EntityId(0, 0, Generator.Integer(600, 800));
        var serials = new long[] { 1, 2 };
        var allowance = new NftAllowance(token, owner, spender, serials, delegatingSpender);
        await Assert.That(allowance.DelegatingSpender).IsEqualTo(delegatingSpender);
        await Assert.That(allowance.SerialNumbers).IsNotNull();
        await Assert.That(allowance.SerialNumbers!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Equivalent_Allowances_Are_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var owner = new EntityId(0, 0, Generator.Integer(200, 400));
        var spender = new EntityId(0, 0, Generator.Integer(400, 600));
        var serials = new long[] { 1, 2, 3 };
        var a1 = new NftAllowance(token, owner, spender, serials);
        var a2 = new NftAllowance(token, owner, spender, serials);
        await Assert.That(a1).IsEqualTo(a2);
        await Assert.That(a1 == a2).IsTrue();
    }
}
