// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class KeyDerivationPathTests
{
    private const uint Hardened = 0x80000000;

    [Test]
    public async Task HashPack_HasEd25519KeyType()
    {
        var keyType = KeyDerivationPath.HashPack.KeyType;
        await Assert.That(keyType).IsEqualTo(KeyType.Ed25519);
    }

    [Test]
    public async Task HashPack_HasFivePathComponents()
    {
        var length = KeyDerivationPath.HashPack.Path.Length;
        var expected = 5;
        await Assert.That(length).IsEqualTo(expected);
    }

    [Test]
    public async Task Calaxy_HasEd25519KeyType()
    {
        var keyType = KeyDerivationPath.Calaxy.KeyType;
        await Assert.That(keyType).IsEqualTo(KeyType.Ed25519);
    }

    [Test]
    public async Task WallaWallet_HasEd25519KeyType()
    {
        var keyType = KeyDerivationPath.WallaWallet.KeyType;
        await Assert.That(keyType).IsEqualTo(KeyType.Ed25519);
    }

    [Test]
    public async Task Blade_HasECDSASecp256K1KeyType()
    {
        var keyType = KeyDerivationPath.Blade.KeyType;
        await Assert.That(keyType).IsEqualTo(KeyType.ECDSASecp256K1);
    }

    [Test]
    public async Task Blade_HasFourPathComponents()
    {
        var length = KeyDerivationPath.Blade.Path.Length;
        var expected = 4;
        await Assert.That(length).IsEqualTo(expected);
    }

    [Test]
    public async Task HashPack_PathStartsWithHardened44()
    {
        var firstComponent = KeyDerivationPath.HashPack.Path.Span[0];
        var expected = 44 | Hardened;
        await Assert.That(firstComponent).IsEqualTo(expected);
    }

    [Test]
    public async Task Blade_PathStartsWithNonHardened44()
    {
        var firstComponent = KeyDerivationPath.Blade.Path.Span[0];
        var expected = 44u;
        await Assert.That(firstComponent).IsEqualTo(expected);
    }
}
