// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8604 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Nft;

public class NftRecordTests
{
    [Test]
    public async Task Constructor_MapsPropertiesCorrectly()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 1000));
        var serial = (long)Generator.Integer(1, 1000);
        var nft = new Hiero.Nft(token, serial);
        await Assert.That(nft.Token).IsEqualTo(token);
        await Assert.That(nft.SerialNumber).IsEqualTo(serial);
    }

    [Test]
    public async Task None_HasZeroValues()
    {
        var none = Hiero.Nft.None;
        await Assert.That(none.Token).IsEqualTo(EntityId.None);
        var expectedSerial = 0L;
        await Assert.That(none.SerialNumber).IsEqualTo(expectedSerial);
    }

    [Test]
    public async Task ToString_ReturnsExpectedFormat()
    {
        var nft = new Hiero.Nft(new EntityId(0, 0, 5), 3);
        var result = nft.ToString();
        await Assert.That(result).IsEqualTo("0.0.5#3");
    }

    [Test]
    public async Task Constructor_NullToken_ThrowsArgumentNullException()
    {
        EntityId token = null;
        var ex = Assert.Throws<ArgumentNullException>(() => { new Hiero.Nft(token, 1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Constructor_NoneToken_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => { new Hiero.Nft(EntityId.None, 1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Constructor_NegativeSerial_ThrowsArgumentOutOfRangeException()
    {
        var token = new EntityId(0, 0, 5);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { new Hiero.Nft(token, -1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task TryParse_ValidString_Succeeds()
    {
        var result = Hiero.Nft.TryParse("0.0.5#3", out var nft);
        await Assert.That(result).IsTrue();
        await Assert.That(nft).IsNotNull();
        var expectedSerial = 3L;
        await Assert.That(nft!.SerialNumber).IsEqualTo(expectedSerial);
    }

    [Test]
    public async Task TryParse_NullString_ReturnsFalse()
    {
        string input = null;
        var result = Hiero.Nft.TryParse(input, out var nft);
        await Assert.That(result).IsFalse();
        await Assert.That(nft).IsNull();
    }

    [Test]
    public async Task TryParse_InvalidFormat_ReturnsFalse()
    {
        var result = Hiero.Nft.TryParse("0.0.5", out var nft);
        await Assert.That(result).IsFalse();
        await Assert.That(nft).IsNull();
    }

    [Test]
    public async Task TryParse_ZeroValues_ReturnsNone()
    {
        var result = Hiero.Nft.TryParse("0.0.0#0", out var nft);
        await Assert.That(result).IsTrue();
        await Assert.That(nft).IsEqualTo(Hiero.Nft.None);
    }

    [Test]
    public async Task ImplicitOperator_ToEntityId_ReturnsToken()
    {
        var token = new EntityId(0, 0, 42);
        var nft = new Hiero.Nft(token, 1);
        EntityId entityId = nft;
        await Assert.That(entityId).IsEqualTo(token);
    }

    [Test]
    public async Task Equality_EquivalentNfts_AreEqual()
    {
        var nft1 = new Hiero.Nft(new EntityId(0, 0, 5), 3);
        var nft2 = new Hiero.Nft(new EntityId(0, 0, 5), 3);
        await Assert.That(nft1).IsEqualTo(nft2);
    }

    [Test]
    public async Task Equality_DifferentNfts_AreNotEqual()
    {
        var nft1 = new Hiero.Nft(new EntityId(0, 0, 5), 3);
        var nft2 = new Hiero.Nft(new EntityId(0, 0, 5), 4);
        await Assert.That(nft1).IsNotEqualTo(nft2);
    }
}
