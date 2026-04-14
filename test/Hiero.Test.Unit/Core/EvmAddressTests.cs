// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602, CS8604 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class EvmAddressTests
{
    [Test]
    public async Task Equivalent_EvmAddresses_Are_Considered_Equal()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EvmAddress(bytes);
        var evmAddress2 = new EvmAddress(bytes);
        await Assert.That(evmAddress1).IsEqualTo(evmAddress2);
        await Assert.That(evmAddress1 == evmAddress2).IsTrue();
        await Assert.That(evmAddress1 != evmAddress2).IsFalse();
        await Assert.That(evmAddress1.Equals(evmAddress2)).IsTrue();
        await Assert.That(evmAddress2.Equals(evmAddress1)).IsTrue();
        await Assert.That(null as EvmAddress == null as EvmAddress).IsTrue();
        await Assert.That(EvmAddress.None.Equals(EvmAddress.None)).IsTrue();
    }

    [Test]
    public async Task Disimilar_EvmAddresses_Are_Not_Considered_Equal()
    {
        var bytes1 = Generator.KeyPair().publicKey[^20..];
        var bytes2 = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EvmAddress(bytes1);
        await Assert.That(evmAddress1).IsNotEqualTo(new EvmAddress(bytes2));
        await Assert.That(evmAddress1 == new EvmAddress(bytes2)).IsFalse();
        await Assert.That(evmAddress1 != new EvmAddress(bytes2)).IsTrue();
    }

    [Test]
    public async Task Comparing_With_Null_Is_Not_Considered_Equal()
    {
        object asNull = null;
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        await Assert.That(evmAddress == null).IsFalse();
        await Assert.That(null == evmAddress).IsFalse();
        await Assert.That(evmAddress != null).IsTrue();
        await Assert.That(evmAddress.Equals(null as EvmAddress)).IsFalse();
        await Assert.That(evmAddress.Equals(asNull)).IsFalse();
        await Assert.That(evmAddress.Equals(EvmAddress.None)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        await Assert.That(evmAddress.Equals("Something that is not an EvmAddress")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        object equivalent = new EvmAddress(bytes);
        await Assert.That(evmAddress.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(evmAddress)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        object reference = evmAddress;
        await Assert.That(evmAddress.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(evmAddress)).IsTrue();
    }

    [Test]
    public async Task Can_Create_Equivalent_EvmAddress_With_Same_Bytes()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EvmAddress(bytes);
        var evmAddress2 = new EvmAddress(bytes);
        await Assert.That(evmAddress1).IsEqualTo(evmAddress2);
        await Assert.That(evmAddress1 == evmAddress2).IsTrue();
        await Assert.That(evmAddress1 != evmAddress2).IsFalse();
        await Assert.That(evmAddress1.Equals(evmAddress2)).IsTrue();
    }

    [Test]
    public async Task Equal_EvmAddresses_Have_Equal_HashCodes()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EvmAddress(bytes);
        var evmAddress2 = new EvmAddress(bytes);
        await Assert.That(evmAddress1.GetHashCode()).IsEqualTo(evmAddress2.GetHashCode());
    }

    [Test]
    public async Task Disimilar_EvmAddresses_Have_Different_HashCodes()
    {
        var bytes1 = Generator.KeyPair().publicKey[^20..];
        var bytes2 = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EvmAddress(bytes1);
        var evmAddress2 = new EvmAddress(bytes2);
        await Assert.That(evmAddress1.GetHashCode()).IsNotEqualTo(evmAddress2.GetHashCode());
    }

    [Test]
    public async Task None_Is_All_Zeros()
    {
        var none = EvmAddress.None;
        await Assert.That(none.Bytes.ToArray().All(b => b == 0)).IsTrue();
    }

    [Test]
    public async Task Bytes_Property_Returns_Correct_Bytes()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        await Assert.That(evmAddress.Bytes.ToArray().SequenceEqual(bytes.ToArray())).IsTrue();
    }

    [Test]
    public async Task Constructor_With_Wrong_Length_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new EvmAddress(new byte[19]);
        });
        await Assert.That(exception.ParamName).IsEqualTo("bytes");
        await Assert.That(exception.Message).StartsWith("The encoded bytes must have a length of 20.");
    }

    [Test]
    public async Task Constructor_From_ECDSA_Endorsement_Creates_Valid_Address()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var evmAddress = new EvmAddress(endorsement);
        await Assert.That(evmAddress.Bytes.Length).IsEqualTo(20);
        await Assert.That(evmAddress).IsNotEqualTo(EvmAddress.None);
    }

    [Test]
    public async Task Constructor_From_Ed25519_Endorsement_Throws_Error()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            new EvmAddress(endorsement);
        });
        await Assert.That(exception.Message).StartsWith("Can only compute a EvmAddress from an Endorsement of type ECDSASecp256K1.");
    }

    [Test]
    public async Task Same_ECDSA_Key_Produces_Same_EvmAddress()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var evmAddress1 = new EvmAddress(endorsement);
        var evmAddress2 = new EvmAddress(endorsement);
        await Assert.That(evmAddress1).IsEqualTo(evmAddress2);
    }

    [Test]
    public async Task ToString_Returns_EIP55_Checksum_Format()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var result = evmAddress.ToString();
        await Assert.That(result).StartsWith("0x");
        await Assert.That(result.Length).IsEqualTo(42);
    }

    [Test]
    public async Task TryParse_Valid_String_With_Prefix()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var str = evmAddress.ToString();
        var success = EvmAddress.TryParse(str, out var parsed);
        await Assert.That(success).IsTrue();
        await Assert.That(parsed).IsEqualTo(evmAddress);
    }

    [Test]
    public async Task TryParse_Valid_String_Without_Prefix()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var str = evmAddress.ToString()[2..]; // strip "0x"
        var success = EvmAddress.TryParse(str, out var parsed);
        await Assert.That(success).IsTrue();
        await Assert.That(parsed).IsEqualTo(evmAddress);
    }

    [Test]
    public async Task TryParse_Invalid_String_Returns_False()
    {
        var success = EvmAddress.TryParse("not-an-address", out var parsed);
        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task TryParse_Null_String_Returns_False()
    {
        var success = EvmAddress.TryParse(null as string, out var parsed);
        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task TryParse_Wrong_Length_Returns_False()
    {
        var success = EvmAddress.TryParse("0x1234", out var parsed);
        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task Implicit_Operator_From_ReadOnlyMemory_Bytes()
    {
        ReadOnlyMemory<byte> bytes = Generator.KeyPair().publicKey[^20..];
        EvmAddress evmAddress = bytes;
        await Assert.That(evmAddress).IsNotNull();
        await Assert.That(evmAddress.Bytes.ToArray().SequenceEqual(bytes.ToArray())).IsTrue();
    }

    [Test]
    public async Task Implicit_Operator_To_EntityId()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        EntityId entityId = evmAddress;
        await Assert.That(entityId).IsNotNull();
        await Assert.That(entityId.ShardNum).IsEqualTo(0L);
        await Assert.That(entityId.RealmNum).IsEqualTo(0L);
    }
}
