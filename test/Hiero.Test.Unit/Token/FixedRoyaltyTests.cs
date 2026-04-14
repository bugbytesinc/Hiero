// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Token;

public class FixedRoyaltyTests
{
    [Test]
    public async Task Constructor_Maps_Properties()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        var token = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var royalty = new FixedRoyalty(receiver, token, amount);
        await Assert.That(royalty.Receiver).IsEqualTo(receiver);
        await Assert.That(royalty.Token).IsEqualTo(token);
        await Assert.That(royalty.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task RoyaltyType_Is_Fixed()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        var token = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var royalty = new FixedRoyalty(receiver, token, amount);
        await Assert.That(royalty.RoyaltyType).IsEqualTo(RoyaltyType.Fixed);
    }

    [Test]
    public async Task Equivalent_Royalties_Are_Equal()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        var token = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var r1 = new FixedRoyalty(receiver, token, amount);
        var r2 = new FixedRoyalty(receiver, token, amount);
        await Assert.That(r1).IsEqualTo(r2);
        await Assert.That(r1 == r2).IsTrue();
    }

    [Test]
    public async Task Different_Royalties_Are_Not_Equal()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        var token = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 1000);
        var r1 = new FixedRoyalty(receiver, token, amount);
        var r2 = new FixedRoyalty(receiver, token, amount + 1);
        await Assert.That(r1).IsNotEqualTo(r2);
        await Assert.That(r1 == r2).IsFalse();
    }
}
