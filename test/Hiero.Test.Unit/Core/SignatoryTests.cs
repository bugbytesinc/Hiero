// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602, CS8604, CS8625 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Proto;

namespace Hiero.Test.Unit.Core;

public class SignatoryTests
{
    [Test]
    public async Task Can_Create_Valid_Signatory_Objects()
    {
        var (_, privateKey1) = Generator.KeyPair();
        var (_, privateKey2) = Generator.KeyPair();

        var sig1 = new Signatory(privateKey1);
        var sig2 = new Signatory(privateKey1, privateKey2);
        var sig3 = new Signatory(new Signatory(privateKey1, privateKey2), new Signatory(privateKey1, privateKey2));

        await Assert.That(sig1).IsNotNull();
        await Assert.That(sig2).IsNotNull();
        await Assert.That(sig3).IsNotNull();
    }

    [Test]
    public async Task Can_Create_Explicit_Ed25519_Signatory_From_DER()
    {
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();

        var signatory = new Signatory(KeyType.Ed25519, privateKey);
        var endorsements = signatory.GetEndorsements();
        await Assert.That(endorsements.Count).IsEqualTo(1);
        await Assert.That(endorsements[0].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(endorsements[0]).IsEqualTo(new Endorsement(publicKey));
    }

    [Test]
    public async Task Explicit_And_Implicit_Ed25519_Signatories_Are_Equal()
    {
        var (_, privateKey) = Generator.Ed25519KeyPair();

        var sigExplicit = new Signatory(KeyType.Ed25519, privateKey);
        var sigImplicit = new Signatory(privateKey);
        await Assert.That(sigExplicit).IsEqualTo(sigImplicit);
        await Assert.That(sigExplicit == sigImplicit).IsTrue();
        await Assert.That(sigExplicit != sigImplicit).IsFalse();
    }

    [Test]
    public async Task Can_Create_Explicit_ECDSASecp256K1_Signatory()
    {
        var (_, privateKey) = Generator.Secp256k1KeyPair();
        var unencoded = ((ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(privateKey.ToArray())).D.ToByteArray();

        var sig1 = new Signatory(KeyType.ECDSASecp256K1, Hex.ToBytes(Hex.FromBytes(unencoded)));
        var sig2 = new Signatory(KeyType.ECDSASecp256K1, Hex.ToBytes(Hex.FromBytes(privateKey)));
        await Assert.That(sig1).IsEqualTo(sig2);
    }

    [Test]
    public async Task Empty_Signatory_List_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory();
        });
        await Assert.That(exception.ParamName).IsEqualTo("signatories");
        await Assert.That(exception.Message).StartsWith("At least one Signatory in a list is required.");
    }

    [Test]
    public async Task Invalid_Bytes_For_Private_Key_Throws_Error()
    {
        var (_, originalKey) = Generator.KeyPair();
        var invalidKey = originalKey.ToArray();
        invalidKey[0] = 0;
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(KeyType.Ed25519, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The private key does not appear to be encoded as a recognizable Ed25519 format.");

        exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(KeyType.ECDSASecp256K1, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The private key was not provided in a recognizable ECDSA Secp256K1 format.");
    }

    [Test]
    public async Task Invalid_Byte_Length_For_Private_Key_Throws_Error()
    {
        var (_, originalKey) = Generator.KeyPair();
        var invalidKey = originalKey.ToArray().Take(30).ToArray();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(KeyType.Ed25519, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The private key does not appear to be encoded as a recognizable Ed25519 format.");
        exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(KeyType.ECDSASecp256K1, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The private key was not provided in a recognizable ECDSA Secp256K1 format.");
    }

    [Test]
    public async Task Equivalent_Signatories_Are_Considered_Equal()
    {
        var (_, privateKey1) = Generator.KeyPair();
        var (_, privateKey2) = Generator.KeyPair();
        var signatory1 = new Signatory(privateKey1);
        var signatory2 = new Signatory(privateKey1);
        await Assert.That(signatory1).IsEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsTrue();
        await Assert.That(signatory1 != signatory2).IsFalse();

        signatory1 = new Signatory(privateKey1, privateKey2);
        signatory2 = new Signatory(privateKey1, privateKey2);
        await Assert.That(signatory1).IsEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsTrue();
        await Assert.That(signatory1 != signatory2).IsFalse();
    }

    [Test]
    public async Task Disimilar_Signatories_Are_Not_Considered_Equal()
    {
        var (_, privateKey1) = Generator.KeyPair();
        var (_, privateKey2) = Generator.KeyPair();
        var signatory1 = new Signatory(privateKey1);
        var signatory2 = new Signatory(privateKey2);
        await Assert.That(signatory1).IsNotEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsFalse();
        await Assert.That(signatory1 != signatory2).IsTrue();

        signatory1 = new Signatory(privateKey1);
        signatory2 = new Signatory(privateKey1, privateKey2);
        await Assert.That(signatory1).IsNotEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsFalse();
        await Assert.That(signatory1 != signatory2).IsTrue();

        signatory1 = new Signatory(privateKey1, privateKey2);
        signatory2 = new Signatory(privateKey2, privateKey1);
        await Assert.That(signatory1).IsNotEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsFalse();
        await Assert.That(signatory1 != signatory2).IsTrue();
    }

    [Test]
    public async Task Disimilar_Multi_Key_Signatories_Are_Not_Considered_Equal()
    {
        var (_, privateKey1) = Generator.KeyPair();
        var (_, privateKey2) = Generator.KeyPair();
        var (_, privateKey3) = Generator.KeyPair();
        var signatories1 = new Signatory(privateKey1, privateKey2);
        var signatories2 = new Signatory(privateKey2, privateKey3);
        await Assert.That(signatories1).IsNotEqualTo(signatories2);
        await Assert.That(signatories1 == signatories2).IsFalse();
        await Assert.That(signatories1 != signatories2).IsTrue();

        signatories1 = new Signatory(privateKey1, privateKey2, privateKey3);
        signatories2 = new Signatory(privateKey1, privateKey2);
        await Assert.That(signatories1).IsNotEqualTo(signatories2);
        await Assert.That(signatories1 == signatories2).IsFalse();
        await Assert.That(signatories1 != signatories2).IsTrue();

        signatories1 = new Signatory(privateKey2, privateKey3, privateKey1);
        signatories2 = new Signatory(privateKey1, privateKey2, privateKey3);
        await Assert.That(signatories1).IsNotEqualTo(signatories2);
        await Assert.That(signatories1 == signatories2).IsFalse();
        await Assert.That(signatories1 != signatories2).IsTrue();
    }

    [Test]
    public async Task Equivalent_Complex_Signatories_Are_Considered_Equal()
    {
        Func<IInvoice, Task> callback = ctx => { return Task.FromResult(0); };
        var (_, privateKey1) = Generator.KeyPair();
        var (_, privateKey2) = Generator.KeyPair();
        var (_, privateKey3) = Generator.KeyPair();
        var signatory1 = new Signatory(callback);
        var signatory2 = new Signatory(callback);
        await Assert.That(signatory1).IsEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsTrue();
        await Assert.That(signatory1 != signatory2).IsFalse();

        signatory1 = new Signatory(privateKey1, new Signatory(callback));
        signatory2 = new Signatory(privateKey1, callback);
        await Assert.That(signatory1).IsEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsTrue();
        await Assert.That(signatory1 != signatory2).IsFalse();

        signatory1 = new Signatory(privateKey1, callback, new Signatory(privateKey2, privateKey3));
        signatory2 = new Signatory(privateKey1, callback, new Signatory(privateKey2, privateKey3));
        await Assert.That(signatory1).IsEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsTrue();
        await Assert.That(signatory1 != signatory2).IsFalse();
    }

    [Test]
    public async Task Callback_Signatories_Are_Only_Reference_Equal()
    {
        static Task callback1(IInvoice ctx) { return Task.FromResult(0); }
        static Task callback2(IInvoice ctx) { return Task.FromResult(0); }

        var signatory1 = new Signatory(callback1);
        var signatory2 = new Signatory(callback2);
        await Assert.That(signatory1).IsNotEqualTo(signatory2);
        await Assert.That(signatory1 == signatory2).IsFalse();
        await Assert.That(signatory1 != signatory2).IsTrue();
    }

    [Test]
    public async Task Can_Sign_With_Ed25519_DER_Key()
    {
        var derPrivateKey = Hex.ToBytes("302e020100300506032b657004220420a89f2eecc02118bc7f6205b11315e0e0a185a4170fa88f28990b5db93154055a");

        var signatory = new Signatory(derPrivateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair).IsNotNull();
        await Assert.That(sigPair.SignatureCase).IsEqualTo(SignaturePair.SignatureOneofCase.Ed25519);
        await Assert.That(Hex.FromBytes(sigPair.PubKeyPrefix.Memory)).IsEqualTo("b9732ad628cb6c28da0c52a3123af7f2725e7a4df53c36a7fc357334ff6dba37");
    }

    [Test]
    public async Task Ambiguous_Raw_32_Byte_Key_Throws_Error()
    {
        var derPrivateKey = Hex.ToBytes("302e020100300506032b657004220420a89f2eecc02118bc7f6205b11315e0e0a185a4170fa88f28990b5db93154055a");
        var rawPrivateKey = derPrivateKey[^32..];
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(rawPrivateKey);
        });
        await Assert.That(exception.Message).StartsWith("The private key byte length of 32 is ambiguous, unable to determine which type of key this refers to.");
    }

    [Test]
    public async Task Can_Sign_With_Explicit_Ed25519_Raw_32_Byte_Key()
    {
        var derPrivateKey = Hex.ToBytes("302e020100300506032b657004220420a89f2eecc02118bc7f6205b11315e0e0a185a4170fa88f28990b5db93154055a");
        var rawPrivateKey = derPrivateKey[^32..];

        var signatory = new Signatory(KeyType.Ed25519, rawPrivateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair).IsNotNull();
        await Assert.That(sigPair.SignatureCase).IsEqualTo(SignaturePair.SignatureOneofCase.Ed25519);
        await Assert.That(Hex.FromBytes(sigPair.PubKeyPrefix.Memory)).IsEqualTo("b9732ad628cb6c28da0c52a3123af7f2725e7a4df53c36a7fc357334ff6dba37");

        var exported = Hex.FromBytes(signatory.GetEndorsements()[0].ToBytes(KeyFormat.Raw));
        await Assert.That(exported).IsEqualTo("b9732ad628cb6c28da0c52a3123af7f2725e7a4df53c36a7fc357334ff6dba37");
    }

    [Test]
    public async Task Can_Sign_With_Secp256K1_DER_Key()
    {
        var (derPublicKey, derPrivateKey) = Generator.Secp256k1KeyPair();

        var signatory = new Signatory(derPrivateKey);
        var compressed = ((ECPublicKeyParameters)PublicKeyFactory.CreateKey(derPublicKey.ToArray())).Q.GetEncoded(true);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair).IsNotNull();
        await Assert.That(sigPair.SignatureCase).IsEqualTo(SignaturePair.SignatureOneofCase.ECDSASecp256K1);
        await Assert.That(Hex.FromBytes(sigPair.PubKeyPrefix.Memory)).IsEqualTo(Hex.FromBytes(compressed));
    }

    [Test]
    public async Task Can_Sign_With_Secp256K1_Hedera_DER_Key()
    {
        var derPrivateKey = Hex.ToBytes("3030020100300706052b8104000a042204200ea81572b0fd122cc9cb90cc57506a2723a2fe1fd7e69c0f26b3c6b6917c60c3");
        var compressed = Hex.ToBytes("02cd51c7f285ffc6c158a4aa866eb6827a61cbe178288df850f26283103a23cc1e");

        var signatory = new Signatory(derPrivateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair).IsNotNull();
        await Assert.That(sigPair.SignatureCase).IsEqualTo(SignaturePair.SignatureOneofCase.ECDSASecp256K1);
        await Assert.That(Hex.FromBytes(sigPair.PubKeyPrefix.Memory)).IsEqualTo(Hex.FromBytes(compressed));
    }

    [Test]
    public async Task Ambiguous_Secp256K1_Raw_32_Byte_Key_Throws_Error()
    {
        var rawPrivateKey = Hex.ToBytes("aa55060f559d5454f596c4b5676e61840add416a49fddab7b7676f8e6899f3e7");
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(rawPrivateKey);
        });
        await Assert.That(exception.Message).StartsWith("The private key byte length of 32 is ambiguous, unable to determine which type of key this refers to.");
    }

    [Test]
    public async Task Can_Sign_With_Explicit_Secp256K1_Raw_32_Byte_Key()
    {
        var rawPrivateKey = Hex.ToBytes("7696d163713ef671481340aa17c825738753fd67b81f9f7e42e4a95c59431cb7");

        var signatory = new Signatory(KeyType.ECDSASecp256K1, rawPrivateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair).IsNotNull();
        await Assert.That(sigPair.SignatureCase).IsEqualTo(SignaturePair.SignatureOneofCase.ECDSASecp256K1);
        await Assert.That(Hex.FromBytes(sigPair.PubKeyPrefix.Memory)).IsEqualTo("032ac21b3fb74a014c3473c51153c590c75fbd969b4b007830bccc7a99c489ab88");

        var exported = Hex.FromBytes(signatory.GetEndorsements()[0].ToBytes(KeyFormat.Raw));
        await Assert.That(exported).IsEqualTo("032ac21b3fb74a014c3473c51153c590c75fbd969b4b007830bccc7a99c489ab88");
    }

    // --- Gap coverage tests ---

    [Test]
    public async Task Comparing_With_Null_Is_Not_Considered_Equal()
    {
        object asNull = null;
        var (_, privateKey) = Generator.KeyPair();
        var signatory = new Signatory(privateKey);
        await Assert.That(signatory == null).IsFalse();
        await Assert.That(null == signatory).IsFalse();
        await Assert.That(signatory != null).IsTrue();
        await Assert.That(signatory.Equals(null as Signatory)).IsFalse();
        await Assert.That(signatory.Equals(asNull)).IsFalse();
    }

    [Test]
    public async Task Null_Null_Signatories_Are_Equal_Via_Operator()
    {
        await Assert.That(null as Signatory == null as Signatory).IsTrue();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var (_, privateKey) = Generator.KeyPair();
        var signatory = new Signatory(privateKey);
        await Assert.That(signatory.Equals("Something that is not a Signatory")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var (_, privateKey) = Generator.KeyPair();
        var signatory = new Signatory(privateKey);
        object equivalent = new Signatory(privateKey);
        await Assert.That(signatory.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(signatory)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var (_, privateKey) = Generator.KeyPair();
        var signatory = new Signatory(privateKey);
        object reference = signatory;
        await Assert.That(signatory.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(signatory)).IsTrue();
    }

    [Test]
    public async Task Equal_Signatories_Have_Equal_HashCodes()
    {
        var (_, privateKey) = Generator.KeyPair();
        var signatory1 = new Signatory(privateKey);
        var signatory2 = new Signatory(privateKey);
        await Assert.That(signatory1.GetHashCode()).IsEqualTo(signatory2.GetHashCode());
    }

    [Test]
    public async Task Equal_Multi_Key_Signatories_Have_Equal_HashCodes()
    {
        var (_, privateKey1) = Generator.KeyPair();
        var (_, privateKey2) = Generator.KeyPair();
        var signatory1 = new Signatory(privateKey1, privateKey2);
        var signatory2 = new Signatory(privateKey1, privateKey2);
        await Assert.That(signatory1.GetHashCode()).IsEqualTo(signatory2.GetHashCode());
    }

    [Test]
    public async Task Callback_Signatory_HashCode_Is_Consistent()
    {
        Func<IInvoice, Task> callback = ctx => Task.FromResult(0);
        var signatory1 = new Signatory(callback);
        var signatory2 = new Signatory(callback);
        await Assert.That(signatory1.GetHashCode()).IsEqualTo(signatory2.GetHashCode());
    }

    [Test]
    public async Task Implicit_Operator_From_ReadOnlyMemory_Bytes()
    {
        var (_, privateKey) = Generator.Ed25519KeyPair();
        Signatory signatory = privateKey;
        await Assert.That(signatory).IsNotNull();
        var explicit1 = new Signatory(privateKey);
        await Assert.That(signatory).IsEqualTo(explicit1);
    }

    [Test]
    public async Task Implicit_Operator_From_Callback()
    {
        Func<IInvoice, Task> callback = ctx => Task.FromResult(0);
        Signatory signatory = callback;
        await Assert.That(signatory).IsNotNull();
        var explicit1 = new Signatory(callback);
        await Assert.That(signatory).IsEqualTo(explicit1);
    }

    [Test]
    public async Task Null_Signatory_In_List_Throws_Error()
    {
        var (_, privateKey) = Generator.KeyPair();
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            new Signatory(new Signatory(privateKey), null!);
        });
        await Assert.That(exception.ParamName).IsEqualTo("signatories");
        await Assert.That(exception.Message).StartsWith("No signatory within the list may be null.");
    }

    [Test]
    public async Task Null_Callback_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            new Signatory(null as Func<IInvoice, Task>);
        });
        await Assert.That(exception.ParamName).IsEqualTo("signingCallback");
        await Assert.That(exception.Message).StartsWith("The signing callback must not be null.");
    }

    [Test]
    public async Task List_KeyType_In_Typed_Constructor_Throws_Error()
    {
        var (_, privateKey) = Generator.KeyPair();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Signatory(KeyType.List, privateKey);
        });
        await Assert.That(exception.Message).Contains("Only signatories representing a single key are supported");
    }

    [Test]
    public async Task GetEndorsements_Returns_Ed25519_Public_Key()
    {
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();
        var signatory = new Signatory(privateKey);
        var endorsements = signatory.GetEndorsements();
        await Assert.That(endorsements.Count).IsEqualTo(1);
        await Assert.That(endorsements[0].Type).IsEqualTo(KeyType.Ed25519);
        var expectedEndorsement = new Endorsement(publicKey);
        await Assert.That(endorsements[0]).IsEqualTo(expectedEndorsement);
    }

    [Test]
    public async Task GetEndorsements_Returns_Secp256K1_Public_Key()
    {
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var signatory = new Signatory(privateKey);
        var endorsements = signatory.GetEndorsements();
        await Assert.That(endorsements.Count).IsEqualTo(1);
        await Assert.That(endorsements[0].Type).IsEqualTo(KeyType.ECDSASecp256K1);
    }

    [Test]
    public async Task GetEndorsements_From_List_Returns_All_Keys()
    {
        var (_, privateKey1) = Generator.Ed25519KeyPair();
        var (_, privateKey2) = Generator.Secp256k1KeyPair();
        var signatory = new Signatory(privateKey1, privateKey2);
        var endorsements = signatory.GetEndorsements();
        await Assert.That(endorsements.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetEndorsements_From_Callback_Returns_Empty_List()
    {
        Func<IInvoice, Task> callback = ctx => Task.FromResult(0);
        var signatory = new Signatory(callback);
        var endorsements = signatory.GetEndorsements();
        await Assert.That(endorsements.Count).IsEqualTo(0);
    }
}
