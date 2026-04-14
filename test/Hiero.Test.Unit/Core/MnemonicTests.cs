// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class MnemonicTests
{
    [Test]
    public async Task Can_Generate_HashPack_Ed25519_Key_From_Mnemonic()
    {
        var words = "absorb radio panic chunk rough person burden place dune submit wagon strong dog coyote more multiply noble impulse fiscal coach cook door goat judge".Split(" ");
        var mnemonic = new Mnemonic(words, "");
        var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.HashPack);

        var expectedPublicKey = Hex.ToBytes("302a300506032b6570032100e8fbc76f27d87092a1f37fc53caf0c084ac2df1b2693c0bef24e350b0843b39f");
        var expectedPrivateKey = Hex.ToBytes("302e020100300506032b6570042204200bfe68bfa4b8048a6c6cd67c33a482ac01b061810f3b443ddf1e15919bd39bfe");

        await Assert.That(publicKey.ToArray().SequenceEqual(expectedPublicKey.ToArray())).IsTrue();
        await Assert.That(privateKey.ToArray().SequenceEqual(expectedPrivateKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Generate_Calaxy_Ed25519_Key_From_Mnemonic()
    {
        var words = "recipe harsh clever agent snow diagram rain use hybrid demand pumpkin dynamic".Split(" ");
        var expectedEndorsement = new Endorsement(KeyType.Ed25519, Hex.ToBytes("1df12df4508a18239333240a92f97287bf1f49ec8f9ff88e6dc996d3b7ab6de7"));
        var mnemonic = new Mnemonic(words, "");
        var (publicKey, _) = mnemonic.GenerateKeyPair(KeyDerivationPath.Calaxy);
        var endorsement = new Endorsement(publicKey);

        await Assert.That(endorsement).IsEqualTo(expectedEndorsement);
    }

    [Test]
    public async Task Can_Generate_Blade_ECDSA_Secp256K1_Key_From_Mnemonic()
    {
        var words = "pass tumble pencil hat grape abstract apple shallow leg embrace truth royal".Split(" ");
        var expectedSignatory = new Signatory(KeyType.ECDSASecp256K1, Hex.ToBytes("13fa7ab3d0b22d66e940b5147af4059408a1ce51ae2415641daad400d2a3f3fb"));
        var expectedEndorsement = new Endorsement(KeyType.ECDSASecp256K1, Hex.ToBytes("0377abfad2ff0e83b10e64852565ffe2296aad79c7e83218672b8719f3dbaeda90"));
        var expectedEvmAddress = new EvmAddress(Hex.ToBytes("69a5b0c36547c5cccce4bbf662e4594ebc2e0a00"));

        var mnemonic = new Mnemonic(words, "");
        var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.Blade);

        var signatory = new Signatory(privateKey);
        var endorsement = new Endorsement(publicKey);
        var evmAddress = new EvmAddress(endorsement);

        await Assert.That(signatory).IsEqualTo(expectedSignatory);
        await Assert.That(endorsement).IsEqualTo(expectedEndorsement);
        await Assert.That(evmAddress).IsEqualTo(expectedEvmAddress);
    }

    [Test]
    public async Task Can_Generate_WallaWallet_Ed25519_Key_From_Mnemonic()
    {
        var words = "flame tower stand popular farm response vacant theory ticket enemy priority wreck".Split(" ");
        var expectedSignatory = new Signatory(KeyType.Ed25519, Hex.ToBytes("431ec28f4a7907fd4bdfa4a02b054addc0c5c20f93dfa34c8e5dfe96f9c0924b"));
        var expectedEndorsement = new Endorsement(KeyType.Ed25519, Hex.ToBytes("6a74a7d3498a3ea407830f31c2b1a6ed15140c5a0934c85a029b2d1fc1d9dfc6"));

        var mnemonic = new Mnemonic(words, "");
        var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.WallaWallet);

        var signatory = new Signatory(privateKey);
        var endorsement = new Endorsement(publicKey);

        await Assert.That(signatory).IsEqualTo(expectedSignatory);
        await Assert.That(endorsement).IsEqualTo(expectedEndorsement);
    }

    [Test]
    public async Task Same_Words_And_Passphrase_Produce_Same_Keys()
    {
        var words = "absorb radio panic chunk rough person burden place dune submit wagon strong dog coyote more multiply noble impulse fiscal coach cook door goat judge".Split(" ");
        var mnemonic1 = new Mnemonic(words, "");
        var mnemonic2 = new Mnemonic(words, "");
        var (pub1, priv1) = mnemonic1.GenerateKeyPair(KeyDerivationPath.HashPack);
        var (pub2, priv2) = mnemonic2.GenerateKeyPair(KeyDerivationPath.HashPack);

        await Assert.That(pub1.ToArray().SequenceEqual(pub2.ToArray())).IsTrue();
        await Assert.That(priv1.ToArray().SequenceEqual(priv2.ToArray())).IsTrue();
    }

    [Test]
    public async Task Different_Passphrase_Produces_Different_Keys()
    {
        var words = "absorb radio panic chunk rough person burden place dune submit wagon strong dog coyote more multiply noble impulse fiscal coach cook door goat judge".Split(" ");
        var mnemonic1 = new Mnemonic(words, "");
        var mnemonic2 = new Mnemonic(words, "secret");
        var (pub1, _) = mnemonic1.GenerateKeyPair(KeyDerivationPath.HashPack);
        var (pub2, _) = mnemonic2.GenerateKeyPair(KeyDerivationPath.HashPack);

        await Assert.That(pub1.ToArray().SequenceEqual(pub2.ToArray())).IsFalse();
    }

    [Test]
    public async Task Null_Passphrase_Is_Treated_As_Empty()
    {
        var words = "absorb radio panic chunk rough person burden place dune submit wagon strong dog coyote more multiply noble impulse fiscal coach cook door goat judge".Split(" ");
        var mnemonic1 = new Mnemonic(words, null!);
        var mnemonic2 = new Mnemonic(words, "");
        var (pub1, _) = mnemonic1.GenerateKeyPair(KeyDerivationPath.HashPack);
        var (pub2, _) = mnemonic2.GenerateKeyPair(KeyDerivationPath.HashPack);

        await Assert.That(pub1.ToArray().SequenceEqual(pub2.ToArray())).IsTrue();
    }

    [Test]
    public async Task KeyDerivationPath_HashPack_Is_Ed25519()
    {
        await Assert.That(KeyDerivationPath.HashPack.KeyType).IsEqualTo(KeyType.Ed25519);
        await Assert.That(KeyDerivationPath.HashPack.Path.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task KeyDerivationPath_Calaxy_Is_Ed25519()
    {
        await Assert.That(KeyDerivationPath.Calaxy.KeyType).IsEqualTo(KeyType.Ed25519);
        await Assert.That(KeyDerivationPath.Calaxy.Path.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task KeyDerivationPath_WallaWallet_Is_Ed25519()
    {
        await Assert.That(KeyDerivationPath.WallaWallet.KeyType).IsEqualTo(KeyType.Ed25519);
        await Assert.That(KeyDerivationPath.WallaWallet.Path.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task KeyDerivationPath_Blade_Is_ECDSASecp256K1()
    {
        await Assert.That(KeyDerivationPath.Blade.KeyType).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(KeyDerivationPath.Blade.Path.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Generated_Ed25519_Keys_Produce_Valid_Signatory_And_Endorsement()
    {
        var words = "absorb radio panic chunk rough person burden place dune submit wagon strong dog coyote more multiply noble impulse fiscal coach cook door goat judge".Split(" ");
        var mnemonic = new Mnemonic(words, "");
        var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.HashPack);

        var signatory = new Signatory(privateKey);
        var endorsement = new Endorsement(publicKey);

        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(signatory).IsNotNull();
        await Assert.That(publicKey.Length).IsGreaterThan(0);
        await Assert.That(privateKey.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Generated_ECDSA_Keys_Produce_Valid_Signatory_And_Endorsement()
    {
        var words = "pass tumble pencil hat grape abstract apple shallow leg embrace truth royal".Split(" ");
        var mnemonic = new Mnemonic(words, "");
        var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.Blade);

        var signatory = new Signatory(privateKey);
        var endorsement = new Endorsement(publicKey);

        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(signatory).IsNotNull();
        await Assert.That(publicKey.Length).IsGreaterThan(0);
        await Assert.That(privateKey.Length).IsGreaterThan(0);
    }
}
