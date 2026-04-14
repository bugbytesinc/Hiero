// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Token;

public class TokenRoyaltyTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var account = new EntityId(0, 0, Generator.Integer(10, 200));
        long numerator = Generator.Integer(1, 50);
        long denominator = Generator.Integer(100, 200);
        long minimum = Generator.Integer(1, 100);
        long maximum = Generator.Integer(500, 1000);
        var royalty = new TokenRoyalty(account, numerator, denominator, minimum, maximum, true);
        await Assert.That(royalty.Receiver).IsEqualTo(account);
        await Assert.That(royalty.Numerator).IsEqualTo(numerator);
        await Assert.That(royalty.Denominator).IsEqualTo(denominator);
        await Assert.That(royalty.Minimum).IsEqualTo(minimum);
        await Assert.That(royalty.Maximum).IsEqualTo(maximum);
        await Assert.That(royalty.AssessAsSurcharge).IsTrue();
    }

    [Test]
    public async Task RoyaltyType_Is_Token()
    {
        var account = new EntityId(0, 0, Generator.Integer(10, 200));
        var royalty = new TokenRoyalty(account, 1, 100, 0, 1000);
        await Assert.That(royalty.RoyaltyType).IsEqualTo(RoyaltyType.Token);
    }

    [Test]
    public async Task AssessAsSurcharge_Defaults_To_False()
    {
        var account = new EntityId(0, 0, Generator.Integer(10, 200));
        var royalty = new TokenRoyalty(account, 1, 100, 0, 1000);
        await Assert.That(royalty.AssessAsSurcharge).IsFalse();
    }

    [Test]
    public async Task AssessAsSurcharge_True_Maps_Correctly()
    {
        var account = new EntityId(0, 0, Generator.Integer(10, 200));
        var royalty = new TokenRoyalty(account, 1, 100, 0, 1000, assessAsSurcharge: true);
        await Assert.That(royalty.AssessAsSurcharge).IsTrue();
    }

    [Test]
    public async Task Equivalent_Royalties_Are_Equal()
    {
        var account = new EntityId(0, 0, Generator.Integer(10, 200));
        long numerator = Generator.Integer(1, 50);
        long denominator = Generator.Integer(100, 200);
        long minimum = Generator.Integer(1, 100);
        long maximum = Generator.Integer(500, 1000);
        var r1 = new TokenRoyalty(account, numerator, denominator, minimum, maximum);
        var r2 = new TokenRoyalty(account, numerator, denominator, minimum, maximum);
        await Assert.That(r1).IsEqualTo(r2);
        await Assert.That(r1 == r2).IsTrue();
    }
}
