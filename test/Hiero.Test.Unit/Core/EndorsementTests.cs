// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602, CS8604 // Null assignments and dereferences are intentional in these tests
using Google.Protobuf;
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Org.BouncyCastle.X509;
using Proto;

namespace Hiero.Test.Unit.Core;

public class EndorsementTests
{
    [Test]
    public async Task Can_Create_Valid_Ed25519_Endorsements()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();
        var (publicKey2, _) = Generator.Ed25519KeyPair();

        var e1 = new Endorsement(publicKey1);
        var e2 = new Endorsement(1, publicKey1);
        var e3 = new Endorsement(publicKey1, publicKey2);
        var e4 = new Endorsement(1, new Endorsement(1, publicKey1, publicKey2), new Endorsement(2, publicKey1, publicKey2));

        await Assert.That(e1).IsNotNull();
        await Assert.That(e2).IsNotNull();
        await Assert.That(e3).IsNotNull();
        await Assert.That(e4).IsNotNull();
    }

    [Test]
    public async Task Can_Create_Valid_ECDSA_Secp256K1_Endorsements()
    {
        var (publicKey1, _) = Generator.Secp256k1KeyPair();
        var (publicKey2, _) = Generator.Secp256k1KeyPair();

        var e1 = new Endorsement(publicKey1);
        var e2 = new Endorsement(1, publicKey1);
        var e3 = new Endorsement(publicKey1, publicKey2);
        var e4 = new Endorsement(1, new Endorsement(1, publicKey1, publicKey2), new Endorsement(2, publicKey1, publicKey2));

        await Assert.That(e1).IsNotNull();
        await Assert.That(e2).IsNotNull();
        await Assert.That(e3).IsNotNull();
        await Assert.That(e4).IsNotNull();
    }

    [Test]
    public async Task Too_Large_Required_Count_Throws_Error()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement((uint)Generator.Integer(2, 4), publicKey);
        });
        await Assert.That(exception.ParamName).IsEqualTo("requiredCount");
        await Assert.That(exception.Message).StartsWith("The required number of keys for a valid signature cannot exceed the number of public keys provided.");
    }

    [Test]
    public async Task Empty_Endorsement_List_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement();
        });
        await Assert.That(exception.ParamName).IsEqualTo("endorsements");
        await Assert.That(exception.Message).StartsWith("At least one endorsement in a list is required.");
    }

    [Test]
    public async Task Invalid_Ed25519_Bytes_Throws_Error()
    {
        var (originalKey, _) = Generator.Ed25519KeyPair();
        var invalidKey = originalKey.ToArray();
        invalidKey[0] = 0;
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.Ed25519, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The public key does not appear to be encoded in a recognizable Ed25519 format.");
    }

    [Test]
    public async Task Invalid_ECDSA_Secp256K1_Bytes_Throws_Error()
    {
        var (originalKey, _) = Generator.Secp256k1KeyPair();
        var invalidKey = originalKey.ToArray();
        invalidKey[0] = 0;
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.ECDSASecp256K1, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The public key was not provided in a recognizable ECDSA Secp256K1 format.");
    }

    [Test]
    public async Task Invalid_Ed25519_Byte_Length_Throws_Error()
    {
        var (originalKey, _) = Generator.Ed25519KeyPair();
        var invalidKey = originalKey.ToArray().Take(30).ToArray();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.Ed25519, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The public key does not appear to be encoded in a recognizable Ed25519 format.");
    }

    [Test]
    public async Task Invalid_ECDSA_Secp256K1_Byte_Length_Throws_Error()
    {
        var (originalKey, _) = Generator.Secp256k1KeyPair();
        var invalidKey = originalKey.ToArray().Take(32).ToArray();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.ECDSASecp256K1, invalidKey);
        });
        await Assert.That(exception.Message).StartsWith("The public key was not provided in a recognizable ECDSA Secp256K1 format.");
    }

    [Test]
    public async Task Equivalent_Ed25519_Endorsements_Are_Considered_Equal()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();
        var (publicKey2, _) = Generator.Ed25519KeyPair();
        var endorsement1 = new Endorsement(publicKey1);
        var endorsement2 = new Endorsement(publicKey1);
        await Assert.That(endorsement1).IsEqualTo(endorsement2);
        await Assert.That(endorsement1 == endorsement2).IsTrue();
        await Assert.That(endorsement1 != endorsement2).IsFalse();

        endorsement1 = new Endorsement(publicKey1, publicKey2);
        endorsement2 = new Endorsement(publicKey1, publicKey2);
        await Assert.That(endorsement1).IsEqualTo(endorsement2);
        await Assert.That(endorsement1 == endorsement2).IsTrue();
        await Assert.That(endorsement1 != endorsement2).IsFalse();

        object asObject1 = endorsement1;
        object asObject2 = endorsement2;
        await Assert.That(asObject1).IsEqualTo(asObject2);
        await Assert.That(endorsement1.Equals(asObject1)).IsTrue();
        await Assert.That(asObject1.Equals(endorsement1)).IsTrue();
    }

    [Test]
    public async Task Equivalent_ECDSA_Secp256K1_Endorsements_Are_Considered_Equal()
    {
        var (publicKey1, _) = Generator.Secp256k1KeyPair();
        var (publicKey2, _) = Generator.Secp256k1KeyPair();
        var endorsement1 = new Endorsement(publicKey1);
        var endorsement2 = new Endorsement(publicKey1);
        await Assert.That(endorsement1).IsEqualTo(endorsement2);
        await Assert.That(endorsement1 == endorsement2).IsTrue();
        await Assert.That(endorsement1 != endorsement2).IsFalse();

        endorsement1 = new Endorsement(publicKey1, publicKey2);
        endorsement2 = new Endorsement(publicKey1, publicKey2);
        await Assert.That(endorsement1).IsEqualTo(endorsement2);
        await Assert.That(endorsement1 == endorsement2).IsTrue();
        await Assert.That(endorsement1 != endorsement2).IsFalse();

        object asObject1 = endorsement1;
        object asObject2 = endorsement2;
        await Assert.That(asObject1).IsEqualTo(asObject2);
        await Assert.That(endorsement1.Equals(asObject1)).IsTrue();
        await Assert.That(asObject1.Equals(endorsement1)).IsTrue();
    }

    [Test]
    public async Task Disimilar_Ed25519_Endorsements_Are_Not_Considered_Equal()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();
        var (publicKey2, _) = Generator.Ed25519KeyPair();
        var endorsements1 = new Endorsement(publicKey1);
        var endorsements2 = new Endorsement(publicKey2);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(publicKey1);
        endorsements2 = new Endorsement(publicKey1, publicKey2);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(publicKey1, publicKey2);
        endorsements2 = new Endorsement(1, publicKey1, publicKey2);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();
    }

    [Test]
    public async Task Disimilar_ECDSA_Secp256K1_Endorsements_Are_Not_Considered_Equal()
    {
        var (publicKey1, _) = Generator.Secp256k1KeyPair();
        var (publicKey2, _) = Generator.Secp256k1KeyPair();
        var endorsements1 = new Endorsement(publicKey1);
        var endorsements2 = new Endorsement(publicKey2);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(publicKey1);
        endorsements2 = new Endorsement(publicKey1, publicKey2);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(publicKey1, publicKey2);
        endorsements2 = new Endorsement(1, publicKey1, publicKey2);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();
    }

    [Test]
    public async Task Disimilar_Multi_Key_Endorsements_Are_Not_Considered_Equal()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();
        var (publicKey2, _) = Generator.Ed25519KeyPair();
        var (publicKey3, _) = Generator.Secp256k1KeyPair();
        var endorsements1 = new Endorsement(publicKey1, publicKey2);
        var endorsements2 = new Endorsement(publicKey2, publicKey3);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(1, publicKey1, publicKey2);
        endorsements2 = new Endorsement(2, publicKey2, publicKey3);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(1, publicKey1, publicKey2, publicKey3);
        endorsements2 = new Endorsement(2, publicKey1, publicKey2, publicKey3);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();

        endorsements1 = new Endorsement(2, publicKey1, publicKey2, publicKey3);
        endorsements2 = new Endorsement(3, publicKey1, publicKey2, publicKey3);
        await Assert.That(endorsements1).IsNotEqualTo(endorsements2);
        await Assert.That(endorsements1 == endorsements2).IsFalse();
        await Assert.That(endorsements1 != endorsements2).IsTrue();
    }

    [Test]
    public async Task Default_Can_Create_Ed25519_Type()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();

        var endorsement = new Endorsement(publicKey1);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(publicKey1.ToArray())).IsTrue();
    }

    [Test]
    public async Task Default_Can_Create_ECDSA_Secp256K1_Type()
    {
        var (publicKey1, _) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(publicKey1);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(publicKey1.ToArray())).IsTrue();
    }

    [Test]
    public async Task Creating_Ed25519_Type_Produces_Ed25519_Type()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();

        var endorsement = new Endorsement(KeyType.Ed25519, publicKey1);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(publicKey1.ToArray())).IsTrue();
    }

    [Test]
    public async Task Creating_ECDSA_Secp256K1_Type_Produces_ECDSA_Secp256K1_Type()
    {
        var (publicKey1, _) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(KeyType.ECDSASecp256K1, publicKey1);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(publicKey1.ToArray())).IsTrue();
    }

    [Test]
    public async Task Creating_Contract_Type_From_Bytes_Throws_Error()
    {
        var contract = new EntityId(Generator.Integer(0, 100), Generator.Integer(0, 100), Generator.Integer(1000, 20000));
        var bytes = Abi.EncodeArguments([contract]);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.Contract, bytes);
        });
        await Assert.That(exception.ParamName).IsEqualTo("type");
        await Assert.That(exception.Message).StartsWith("Only endorsements representing single Ed25519 or ECDSASecp256K1 keys are supported with this constructor, please use the contract address constructor instead.");
    }

    [Test]
    public async Task Can_Create_Contract_Type()
    {
        var contract = new EntityId(Generator.Integer(0, 100), Generator.Integer(0, 100), Generator.Integer(1000, 20000));
        var endorsement = new Endorsement(contract);

        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Contract);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes(KeyFormat.Der).IsEmpty).IsTrue();
        await Assert.That(endorsement.Contract).IsEqualTo(contract);
    }

    [Test]
    public async Task Can_Create_N_Of_M_List()
    {
        var n = (uint)Generator.Integer(1, 4);
        var m = Generator.Integer(5, 10);
        var keys = Enumerable.Range(0, m).Select(i => new Endorsement(Generator.Ed25519KeyPair().publicKey)).ToArray();
        var list = new Endorsement(n, keys);
        await Assert.That(list.Type).IsEqualTo(KeyType.List);
        await Assert.That(list.RequiredCount).IsEqualTo(n);
        await Assert.That(list.List.Length).IsEqualTo(m);
        for (int i = 0; i < m; i++)
        {
            await Assert.That(list.List[i]).IsEqualTo(keys[i]);
        }
    }

    [Test]
    public async Task Can_Enumerate_An_Endorsement_Tree()
    {
        var (publicKey1a, _) = Generator.Ed25519KeyPair();
        var (publicKey2a, _) = Generator.Ed25519KeyPair();
        var (publicKey3a, _) = Generator.Ed25519KeyPair();
        var (publicKey1b, _) = Generator.Ed25519KeyPair();
        var (publicKey2b, _) = Generator.Ed25519KeyPair();
        var (publicKey3b, _) = Generator.Ed25519KeyPair();
        var endorsements1 = new Endorsement(1, publicKey1a, publicKey1b);
        var endorsements2 = new Endorsement(1, publicKey2a, publicKey2b);
        var endorsements3 = new Endorsement(publicKey3a, publicKey3b);
        var tree = new Endorsement(endorsements1, endorsements2, endorsements3);

        await Assert.That(tree.Type).IsEqualTo(KeyType.List);
        await Assert.That(tree.RequiredCount).IsEqualTo(3U);
        await Assert.That(tree.List.Length).IsEqualTo(3);
        await Assert.That(tree.ToBytes(KeyFormat.Der).IsEmpty).IsTrue();

        await Assert.That(tree.List[0].Type).IsEqualTo(KeyType.List);
        await Assert.That(tree.List[0].RequiredCount).IsEqualTo(1U);
        await Assert.That(tree.List[0].List.Length).IsEqualTo(2);
        await Assert.That(tree.List[0].ToBytes(KeyFormat.Der).IsEmpty).IsTrue();

        await Assert.That(tree.List[0].List[0].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(tree.List[0].List[0].RequiredCount).IsEqualTo(0U);
        await Assert.That(tree.List[0].List[0].List).IsEmpty();
        await Assert.That(tree.List[0].List[0].ToBytes().ToArray().SequenceEqual(publicKey1a.ToArray())).IsTrue();

        await Assert.That(tree.List[0].List[1].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(tree.List[0].List[1].RequiredCount).IsEqualTo(0U);
        await Assert.That(tree.List[0].List[1].List).IsEmpty();
        await Assert.That(tree.List[0].List[1].ToBytes().ToArray().SequenceEqual(publicKey1b.ToArray())).IsTrue();

        await Assert.That(tree.List[1].Type).IsEqualTo(KeyType.List);
        await Assert.That(tree.List[1].RequiredCount).IsEqualTo(1U);
        await Assert.That(tree.List[1].List.Length).IsEqualTo(2);
        await Assert.That(tree.List[1].ToBytes(KeyFormat.Der).IsEmpty).IsTrue();

        await Assert.That(tree.List[1].List[0].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(tree.List[1].List[0].RequiredCount).IsEqualTo(0U);
        await Assert.That(tree.List[1].List[0].List).IsEmpty();
        await Assert.That(tree.List[1].List[0].ToBytes().ToArray().SequenceEqual(publicKey2a.ToArray())).IsTrue();

        await Assert.That(tree.List[1].List[1].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(tree.List[1].List[1].RequiredCount).IsEqualTo(0U);
        await Assert.That(tree.List[1].List[1].List).IsEmpty();
        await Assert.That(tree.List[1].List[1].ToBytes().ToArray().SequenceEqual(publicKey2b.ToArray())).IsTrue();

        await Assert.That(tree.List[2].Type).IsEqualTo(KeyType.List);
        await Assert.That(tree.List[2].RequiredCount).IsEqualTo(2U);
        await Assert.That(tree.List[2].List.Length).IsEqualTo(2);
        await Assert.That(tree.List[2].ToBytes(KeyFormat.Der).IsEmpty).IsTrue();

        await Assert.That(tree.List[2].List[0].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(tree.List[2].List[0].RequiredCount).IsEqualTo(0U);
        await Assert.That(tree.List[2].List[0].List).IsEmpty();
        await Assert.That(tree.List[2].List[0].ToBytes().ToArray().SequenceEqual(publicKey3a.ToArray())).IsTrue();

        await Assert.That(tree.List[2].List[1].Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(tree.List[2].List[1].RequiredCount).IsEqualTo(0U);
        await Assert.That(tree.List[2].List[1].List).IsEmpty();
        await Assert.That(tree.List[2].List[1].ToBytes().ToArray().SequenceEqual(publicKey3b.ToArray())).IsTrue();
    }

    [Test]
    public async Task Make_List_Type_From_Key_Type_Constructor_Throws_Error()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.List, publicKey);
        });
        await Assert.That(exception.ParamName).IsEqualTo("type");
        await Assert.That(exception.Message).StartsWith("Only endorsements representing single Ed25519 or ECDSASecp256K1 keys are supported with this constructor, please use the list constructor instead.");
    }

    [Test]
    public async Task Make_Contract_Type_From_Key_Type_Constructor_Throws_Error()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new Endorsement(KeyType.Contract, publicKey);
        });
        await Assert.That(exception.ParamName).IsEqualTo("type");
        await Assert.That(exception.Message).StartsWith("Only endorsements representing single Ed25519 or ECDSASecp256K1 keys are supported with this constructor, please use the contract address constructor instead.");
    }

    [Test]
    public async Task Equivalent_Contract_Types_Are_Considered_Equal()
    {
        var contract = new EntityId(Generator.Integer(0, 100), Generator.Integer(0, 100), Generator.Integer(1000, 20000));

        var endorsement1 = new Endorsement(contract);
        var endorsement2 = new Endorsement(new EntityId(contract.ShardNum, contract.RealmNum, contract.AccountNum));
        await Assert.That(endorsement1).IsEqualTo(endorsement2);
        await Assert.That(endorsement1 == endorsement2).IsTrue();
        await Assert.That(endorsement1 != endorsement2).IsFalse();

        object asObject1 = endorsement1;
        object asObject2 = endorsement2;
        await Assert.That(asObject1).IsEqualTo(asObject2);
        await Assert.That(endorsement1.Equals(asObject2)).IsTrue();
        await Assert.That(asObject1.Equals(endorsement2)).IsTrue();
    }

    [Test]
    public async Task Disimilar_Contract_Types_Are_Not_Considered_Equal()
    {
        var contract1 = new EntityId(Generator.Integer(0, 100), Generator.Integer(0, 100), Generator.Integer(1000, 20000));
        var contract2 = new EntityId(Generator.Integer(0, 100), Generator.Integer(0, 100), Generator.Integer(20001, 50000));

        var endorsement1 = new Endorsement(contract1);
        var endorsement2 = new Endorsement(contract2);
        await Assert.That(endorsement1).IsNotEqualTo(endorsement2);
        await Assert.That(endorsement1 == endorsement2).IsFalse();
        await Assert.That(endorsement1 != endorsement2).IsTrue();

        object asObject1 = endorsement1;
        object asObject2 = endorsement2;
        await Assert.That(asObject1).IsNotEqualTo(asObject2);
        await Assert.That(endorsement1.Equals(asObject2)).IsFalse();
        await Assert.That(asObject1.Equals(endorsement2)).IsFalse();
    }

    [Test]
    public async Task Contract_Protobuf_Key_Can_Create_Contract_Type()
    {
        var contract = new EntityId(Generator.Integer(0, 100), Generator.Integer(0, 100), Generator.Integer(1000, 20000));
        var contractID = new Proto.ContractID(contract);
        var key = new Proto.Key { ContractID = contractID };
        var endorsement = key.ToEndorsement();

        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Contract);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.Contract).IsEqualTo(contract);
    }

    [Test]
    public async Task Can_Parse_Ed25519_Der_Encoded()
    {
        var publicKey = Hex.ToBytes("302a300506032b65700321001dd944db2def347f51ef46ab7bafba05e139ed3cadfa9786ce6ab034284d500d");

        var endorsement = new Endorsement(publicKey);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(publicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Parse_Ed25519_Raw_32_Byte_Key()
    {
        var derPublicKey = Hex.ToBytes("302a300506032b65700321001dd944db2def347f51ef46ab7bafba05e139ed3cadfa9786ce6ab034284d500d");
        var rawPublicKey = derPublicKey[^32..];

        var endorsement = new Endorsement(rawPublicKey);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Parse_Secp256K1_From_Extended_Der_Encoding()
    {
        var (derPublicKey, _) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(derPublicKey);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Parse_Secp256K1_From_Compacted_Der_Encoding()
    {
        var derPublicKey = Hex.ToBytes("302d300706052b8104000a03220002ffd5a91eb6e55f584718a7da0bc168cddf9dd3dec2a968e574181a8fd9ab95ae");
        var longFormKey = Hex.ToBytes("308201333081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f3044042000000000000000000000000000000000000000000000000000000000000000000420000000000000000000000000000000000000000000000000000000000000000704410479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd036414102010103420004ffd5a91eb6e55f584718a7da0bc168cddf9dd3dec2a968e574181a8fd9ab95aee07205037c7be54a4b818c79eeec0a44e502a12abf2641e06554d643b7fb4516");

        var endorsement = new Endorsement(derPublicKey);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(longFormKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Parse_Secp256K1_From_Raw_Form()
    {
        var derPublicKey = Hex.ToBytes("02ffd5a91eb6e55f584718a7da0bc168cddf9dd3dec2a968e574181a8fd9ab95ae");
        var longFormKey = Hex.ToBytes("308201333081ec06072a8648ce3d02013081e0020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f3044042000000000000000000000000000000000000000000000000000000000000000000420000000000000000000000000000000000000000000000000000000000000000704410479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd036414102010103420004ffd5a91eb6e55f584718a7da0bc168cddf9dd3dec2a968e574181a8fd9ab95aee07205037c7be54a4b818c79eeec0a44e502a12abf2641e06554d643b7fb4516");

        var endorsement = new Endorsement(derPublicKey);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
        await Assert.That(endorsement.ToBytes().ToArray().SequenceEqual(longFormKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Extract_Ed25519_Bytes_In_Various_Formats()
    {
        var derPublicKey = Hex.ToBytes("302a300506032b6570032100eeed21c291ef1d6860540370e9907ea9a7cb529dba1c0bfaa6dcf644f28aab31");
        var rawPublicKey = derPublicKey[^32..];
        var prtPublicKey = (new Proto.Key { Ed25519 = ByteString.CopyFrom(rawPublicKey.ToArray()) }).ToByteString().Memory;

        var endorsement = new Endorsement(rawPublicKey);

        await Assert.That(endorsement.ToBytes(KeyFormat.Default).ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Raw).ToArray().SequenceEqual(rawPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Der).ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Protobuf).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Hedera).ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Mirror).ToArray().SequenceEqual(rawPublicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Extract_Secp256K1_Bytes_In_Various_Formats()
    {
        ReadOnlyMemory<byte> rawPublicKey = Hex.ToBytes("026866c9664a95af2e9d8e7109eb8ccbe74eb822d49be1242b1511d775d1826e2a");
        ReadOnlyMemory<byte> hdrPublicKey = Hex.ToBytes("302d300706052b8104000a032200026866c9664a95af2e9d8e7109eb8ccbe74eb822d49be1242b1511d775d1826e2a");
        ReadOnlyMemory<byte> derPublicKey = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(KeyUtils.ParsePublicEcdsaSecp256k1Key(rawPublicKey)).GetDerEncoded();
        ReadOnlyMemory<byte> prtPublicKey = (new Proto.Key { ECDSASecp256K1 = ByteString.CopyFrom(rawPublicKey.ToArray()) }).ToByteString().Memory;

        var endorsement = new Endorsement(KeyType.ECDSASecp256K1, rawPublicKey);

        await Assert.That(endorsement.ToBytes(KeyFormat.Default).ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Raw).ToArray().SequenceEqual(rawPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Der).ToArray().SequenceEqual(derPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Protobuf).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Hedera).ToArray().SequenceEqual(hdrPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Mirror).ToArray().SequenceEqual(rawPublicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Extract_Contract_Bytes_In_Various_Formats()
    {
        var contract = new EntityId(Generator.Integer(0, 5), Generator.Integer(1, 5), Generator.Integer(1001, 1000000));

        ReadOnlyMemory<byte> prtPublicKey = (new Proto.Key { ContractID = new Proto.ContractID(contract) }).ToByteString().Memory;

        var endorsement = new Endorsement(contract);

        await Assert.That(endorsement.ToBytes(KeyFormat.Default).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Raw).IsEmpty).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Der).IsEmpty).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Protobuf).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Hedera).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Mirror).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Extract_List_Bytes_In_Various_Formats()
    {
        var (derPublicKey, _) = Generator.Secp256k1KeyPair();
        var inner = new Endorsement(derPublicKey);
        var endorsement = new Endorsement(1, inner);

        ReadOnlyMemory<byte> prtPublicKey = (new Proto.Key(endorsement)).ToByteString().Memory;

        await Assert.That(endorsement.ToBytes(KeyFormat.Default).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Raw).IsEmpty).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Der).IsEmpty).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Protobuf).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Hedera).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
        await Assert.That(endorsement.ToBytes(KeyFormat.Mirror).ToArray().SequenceEqual(prtPublicKey.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Verify_Valid_Ed25519_Signature()
    {
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Secp256k1KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        var signature = sigMap.SigPair[0].Ed25519.ToByteArray();

        await Assert.That(endorsement.Verify(message, signature)).IsTrue();
    }

    [Test]
    public async Task Cannot_Verify_Invalid_Ed25519_Signature()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var (_, privateKey) = Generator.Ed25519KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Secp256k1KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        var signature = sigMap.SigPair[0].Ed25519.ToByteArray();

        await Assert.That(endorsement.Verify(message, signature)).IsFalse();
    }

    [Test]
    public async Task Can_Verify_Valid_Secp256K1_Signature()
    {
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        var signature = sigMap.SigPair[0].ECDSASecp256K1.ToByteArray();

        await Assert.That(endorsement.Verify(message, signature)).IsTrue();
    }

    [Test]
    public async Task Cannot_Verify_Invalid_Secp256K1_Signature()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var (_, privateKey) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        var signature = sigMap.SigPair[0].ECDSASecp256K1.ToByteArray();

        await Assert.That(endorsement.Verify(message, signature)).IsFalse();
    }

    [Test]
    public async Task Attempt_To_Verify_List_Endorsement_Raises_Error()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();

        var endorsement = new Endorsement(new Endorsement(publicKey1), new Endorsement(publicKey2));
        var signatory = new Signatory(new Signatory(privateKey1), new Signatory(privateKey2));
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        var signature = sigMap.SigPair[0].ECDSASecp256K1?.ToByteArray() ?? sigMap.SigPair[0].Ed25519.ToByteArray();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            endorsement.Verify(message, signature);
        });
        await Assert.That(ex.Message).StartsWith("Only endorsements representing single Ed25519 or ECDSASecp256K1 keys support validation of signatures, use SigPair.Satisfies for complex public key types.");
    }

    [Test]
    public async Task Attempt_To_Verify_Contract_Endorsement_Raises_Error()
    {
        var (_, privateKey) = Generator.KeyPair();

        var endorsement = new Endorsement(new EntityId(0, 0, Generator.Integer(1000, 2000)));
        var signatory = new Signatory(privateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        var signature = sigMap.SigPair[0].ECDSASecp256K1?.ToByteArray() ?? sigMap.SigPair[0].Ed25519.ToByteArray();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            endorsement.Verify(message, signature);
        });
        await Assert.That(ex.Message).StartsWith("Only endorsements representing single Ed25519 or ECDSASecp256K1 keys support validation of signatures, unable to validate Contract key type.");
    }

    [Test]
    public async Task None_Endorsement_Has_Expected_Properties()
    {
        var none = Endorsement.None;
        await Assert.That(none.Type).IsEqualTo(KeyType.List);
        await Assert.That(none.List).IsEmpty();
        await Assert.That(none.RequiredCount).IsEqualTo(0U);
        await Assert.That(none.Contract).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task ToString_Returns_None_For_None_Endorsement()
    {
        await Assert.That(Endorsement.None.ToString()).IsEqualTo("None");
    }

    [Test]
    public async Task ToString_Returns_Hex_For_Key_Endorsement()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        var result = endorsement.ToString();
        await Assert.That(result).StartsWith("0x");
        await Assert.That(result.Length).IsGreaterThan(2);
    }

    [Test]
    public async Task Equal_Endorsements_Have_Equal_HashCodes()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement1 = new Endorsement(publicKey);
        var endorsement2 = new Endorsement(publicKey);
        await Assert.That(endorsement1.GetHashCode()).IsEqualTo(endorsement2.GetHashCode());
    }

    [Test]
    public async Task Equal_Secp256K1_Endorsements_Have_Equal_HashCodes()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var endorsement1 = new Endorsement(publicKey);
        var endorsement2 = new Endorsement(publicKey);
        await Assert.That(endorsement1.GetHashCode()).IsEqualTo(endorsement2.GetHashCode());
    }

    [Test]
    public async Task Equal_Contract_Endorsements_Have_Equal_HashCodes()
    {
        var contract = new EntityId(0, 0, Generator.Integer(1000, 2000));
        var endorsement1 = new Endorsement(contract);
        var endorsement2 = new Endorsement(new EntityId(contract.ShardNum, contract.RealmNum, contract.AccountNum));
        await Assert.That(endorsement1.GetHashCode()).IsEqualTo(endorsement2.GetHashCode());
    }

    [Test]
    public async Task Equal_List_Endorsements_Have_Equal_HashCodes()
    {
        var (publicKey1, _) = Generator.Ed25519KeyPair();
        var (publicKey2, _) = Generator.Ed25519KeyPair();
        var endorsement1 = new Endorsement(1, new Endorsement(publicKey1), new Endorsement(publicKey2));
        var endorsement2 = new Endorsement(1, new Endorsement(publicKey1), new Endorsement(publicKey2));
        await Assert.That(endorsement1.GetHashCode()).IsEqualTo(endorsement2.GetHashCode());
    }

    [Test]
    public async Task Implicit_Operator_Creates_Endorsement_From_Bytes()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        Endorsement endorsement = publicKey;
        await Assert.That(endorsement).IsNotNull();
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(endorsement).IsEqualTo(new Endorsement(publicKey));
    }

    [Test]
    public async Task Implicit_Operator_Creates_EntityId_From_Endorsement()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        EntityId entityId = endorsement;
        await Assert.That(entityId).IsNotNull();
        await Assert.That(entityId.ShardNum).IsEqualTo(0L);
        await Assert.That(entityId.RealmNum).IsEqualTo(0L);
    }

    [Test]
    public async Task Equals_Null_Endorsement_Returns_False()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        await Assert.That(endorsement.Equals(null as Endorsement)).IsFalse();
    }

    [Test]
    public async Task Equals_Object_Null_Returns_False()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        await Assert.That(endorsement.Equals(null as object)).IsFalse();
    }

    [Test]
    public async Task Equals_Reference_Same_Returns_True()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        object reference = endorsement;
        await Assert.That(endorsement.Equals(reference)).IsTrue();
    }

    [Test]
    public async Task Equals_Non_Endorsement_Object_Returns_False()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        await Assert.That(endorsement.Equals("not an endorsement")).IsFalse();
    }

    [Test]
    public async Task Operator_Equals_With_Null_Left_And_Null_Right()
    {
        Endorsement left = null;
        Endorsement right = null;
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
    }

    [Test]
    public async Task Operator_Equals_With_Null_Left_And_Non_Null_Right()
    {
        Endorsement left = null;
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var right = new Endorsement(publicKey);
        await Assert.That(left == right).IsFalse();
        await Assert.That(left != right).IsTrue();
    }

    [Test]
    public async Task Null_Endorsement_In_List_Throws_Error()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            new Endorsement(new Endorsement(publicKey), null!);
        });
        await Assert.That(exception.ParamName).IsEqualTo("endorsements");
        await Assert.That(exception.Message).StartsWith("No endorsement within the list may be null.");
    }

    [Test]
    public async Task Special_Protobuf_None_Bytes_Creates_Empty_List()
    {
        ReadOnlyMemory<byte> noneBytes = new byte[] { 50, 0 };
        var endorsement = new Endorsement(noneBytes);
        await Assert.That(endorsement.Type).IsEqualTo(KeyType.List);
        await Assert.That(endorsement.List).IsEmpty();
        await Assert.That(endorsement.RequiredCount).IsEqualTo(0U);
    }

    [Test]
    public async Task Contract_Property_Returns_None_For_Non_Contract_Types()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        await Assert.That(endorsement.Contract).IsEqualTo(EntityId.None);

        var listEndorsement = new Endorsement(1, new Endorsement(publicKey));
        await Assert.That(listEndorsement.Contract).IsEqualTo(EntityId.None);
    }
}
