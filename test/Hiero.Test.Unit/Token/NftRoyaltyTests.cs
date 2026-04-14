// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Token;

public class NftRoyaltyTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        long numerator = Generator.Integer(1, 50);
        long denominator = Generator.Integer(100, 200);
        long fallbackAmount = Generator.Integer(500, 1000);
        var fallbackToken = new EntityId(0, 0, Generator.Integer(200, 400));
        var royalty = new NftRoyalty(receiver, numerator, denominator, fallbackAmount, fallbackToken);
        await Assert.That(royalty.Receiver).IsEqualTo(receiver);
        await Assert.That(royalty.Numerator).IsEqualTo(numerator);
        await Assert.That(royalty.Denominator).IsEqualTo(denominator);
        await Assert.That(royalty.FallbackAmount).IsEqualTo(fallbackAmount);
        await Assert.That(royalty.FallbackToken).IsEqualTo(fallbackToken);
    }

    [Test]
    public async Task RoyaltyType_Is_Nft()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        var fallbackToken = new EntityId(0, 0, Generator.Integer(200, 400));
        var royalty = new NftRoyalty(receiver, 1, 100, 50, fallbackToken);
        await Assert.That(royalty.RoyaltyType).IsEqualTo(RoyaltyType.Nft);
    }

    [Test]
    public async Task Equivalent_Royalties_Are_Equal()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        long numerator = Generator.Integer(1, 50);
        long denominator = Generator.Integer(100, 200);
        long fallbackAmount = Generator.Integer(500, 1000);
        var fallbackToken = new EntityId(0, 0, Generator.Integer(200, 400));
        var r1 = new NftRoyalty(receiver, numerator, denominator, fallbackAmount, fallbackToken);
        var r2 = new NftRoyalty(receiver, numerator, denominator, fallbackAmount, fallbackToken);
        await Assert.That(r1).IsEqualTo(r2);
        await Assert.That(r1 == r2).IsTrue();
    }

    [Test]
    public async Task Different_Royalties_Are_Not_Equal()
    {
        var receiver = new EntityId(0, 0, Generator.Integer(10, 200));
        var fallbackToken = new EntityId(0, 0, Generator.Integer(200, 400));
        var r1 = new NftRoyalty(receiver, 1, 100, 50, fallbackToken);
        var r2 = new NftRoyalty(receiver, 2, 100, 50, fallbackToken);
        await Assert.That(r1).IsNotEqualTo(r2);
        await Assert.That(r1 == r2).IsFalse();
    }
}
