// SPDX-License-Identifier: Apache-2.0
// Null assignments and dereferences are intentional in these tests
#pragma warning disable CS8600, CS8602, CS8604 
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class EntityIdTests
{
    [Test]
    public async Task Equivalent_EntityIds_Are_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId1 = new EntityId(shardNum, realmNum, accountNum);
        var entityId2 = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId1).IsEqualTo(entityId2);
        await Assert.That(entityId1 == entityId2).IsTrue();
        await Assert.That(entityId1 != entityId2).IsFalse();
        await Assert.That(entityId1.Equals(entityId2)).IsTrue();
        await Assert.That(entityId2.Equals(entityId1)).IsTrue();
        await Assert.That(null as EntityId == null as EntityId).IsTrue();
    }

    [Test]
    public async Task Disimilar_EntityIds_Are_Not_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId1 = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId1).IsNotEqualTo(new EntityId(shardNum, realmNum + 1, accountNum));
        await Assert.That(entityId1).IsNotEqualTo(new EntityId(shardNum + 1, realmNum, accountNum));
        await Assert.That(entityId1).IsNotEqualTo(new EntityId(shardNum, realmNum, accountNum + 1));
        await Assert.That(entityId1 == new EntityId(shardNum, realmNum, accountNum + 1)).IsFalse();
        await Assert.That(entityId1 != new EntityId(shardNum, realmNum, accountNum + 1)).IsTrue();
        await Assert.That(entityId1.Equals(new EntityId(shardNum + 1, realmNum, accountNum))).IsFalse();
        await Assert.That(entityId1.Equals(new EntityId(shardNum, realmNum + 1, accountNum))).IsFalse();
        await Assert.That(entityId1.Equals(new EntityId(shardNum, realmNum, accountNum + 1))).IsFalse();

        await Assert.That(entityId1.TryGetKeyAlias(out Endorsement alias)).IsFalse();
        await Assert.That(alias).IsNull();

        await Assert.That(entityId1.TryGetEvmAddress(out EvmAddress evmAddress)).IsFalse();
        await Assert.That(evmAddress).IsNull();
    }

    [Test]
    public async Task Null_EntityIds_Are_Not_Considered_Equal()
    {
        object asNull = null;
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId == null).IsFalse();
        await Assert.That(null == entityId).IsFalse();
        await Assert.That(entityId != null).IsTrue();
        await Assert.That(entityId.Equals(null)).IsFalse();
        await Assert.That(entityId.Equals(asNull)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId.Equals("Something that is not an entityId")).IsFalse();
    }

    [Test]
    public async Task EntityId_Cast_As_Object_Is_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        object equivalent = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(entityId)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        object reference = entityId;
        await Assert.That(entityId.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(entityId)).IsTrue();
    }

    [Test]
    public async Task Equivalent_Alias_EntityIds_Are_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var (publicKey, _) = Generator.KeyPair();
        var alias1 = new EntityId(shardNum, realmNum, (Endorsement)publicKey);
        var alias2 = new EntityId(shardNum, realmNum, (Endorsement)publicKey);
        await Assert.That(alias1).IsEqualTo(alias2);
        await Assert.That(alias1 == alias2).IsTrue();
        await Assert.That(alias1 != alias2).IsFalse();
        await Assert.That(alias1.Equals(alias2)).IsTrue();
        await Assert.That(alias2.Equals(alias1)).IsTrue();
        await Assert.That(null as EntityId == null as EntityId).IsTrue();
    }

    [Test]
    public async Task Disimilar_Alias_EntityIds_Are_Not_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var (publicKey1, _) = Generator.KeyPair();
        var (publicKey2, _) = Generator.KeyPair();
        var alias1 = new EntityId(shardNum, realmNum, (Endorsement)publicKey1);
        await Assert.That(alias1).IsNotEqualTo(new EntityId(shardNum, realmNum + 1, (Endorsement)publicKey1));
        await Assert.That(alias1).IsNotEqualTo(new EntityId(shardNum + 1, realmNum, (Endorsement)publicKey1));
        await Assert.That(alias1).IsNotEqualTo(new EntityId(shardNum, realmNum, (Endorsement)publicKey2));
        await Assert.That(alias1 == new EntityId(shardNum, realmNum, (Endorsement)publicKey2)).IsFalse();
        await Assert.That(alias1 != new EntityId(shardNum, realmNum, (Endorsement)publicKey2)).IsTrue();
        await Assert.That(alias1.Equals(new EntityId(shardNum + 1, realmNum, (Endorsement)publicKey1))).IsFalse();
        await Assert.That(alias1.Equals(new EntityId(shardNum, realmNum + 1, (Endorsement)publicKey1))).IsFalse();
        await Assert.That(alias1.Equals(new EntityId(shardNum, realmNum, (Endorsement)publicKey2))).IsFalse();
    }

    [Test]
    public async Task Null_Alias_EntityIds_Are_Not_Considered_Equal()
    {
        object asNull = null;
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var (publicKey, _) = Generator.KeyPair();
        var alias = new EntityId(shardNum, realmNum, (Endorsement)publicKey);
        await Assert.That(alias == null).IsFalse();
        await Assert.That(null == alias).IsFalse();
        await Assert.That(alias != null).IsTrue();
        await Assert.That(alias.Equals(null)).IsFalse();
        await Assert.That(alias.Equals(asNull)).IsFalse();
    }

    [Test]
    public async Task Alias_Cast_As_Object_Is_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var (publicKey, _) = Generator.KeyPair();
        var alias = new EntityId(shardNum, realmNum, (Endorsement)publicKey);
        object equivalent = new EntityId(shardNum, realmNum, (Endorsement)publicKey);
        await Assert.That(alias.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(alias)).IsTrue();
    }

    [Test]
    public async Task Alias_Reference_Equal_Is_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var (publicKey, _) = Generator.KeyPair();
        var alias = new EntityId(shardNum, realmNum, (Endorsement)publicKey);
        object reference = alias;
        await Assert.That(alias.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(alias)).IsTrue();
    }

    [Test]
    public async Task Equivalent_Alias_EntityIds_With_Different_Constructors()
    {
        var (publicKey, _) = Generator.KeyPair();
        var endorsement = new Endorsement(publicKey);
        var alias1 = (EntityId)(Endorsement)publicKey;
        var alias2 = (EntityId)endorsement;
        var alias3 = new EntityId(0, 0, (Endorsement)publicKey);
        var alias4 = new EntityId(0, 0, endorsement);
        await Assert.That(alias1).IsEqualTo(alias2);
        await Assert.That(alias1).IsEqualTo(alias3);
        await Assert.That(alias1).IsEqualTo(alias4);
        await Assert.That(alias2).IsEqualTo(alias3);
        await Assert.That(alias2).IsEqualTo(alias4);
        await Assert.That(alias3).IsEqualTo(alias4);
        await Assert.That(alias1 == alias2).IsTrue();
        await Assert.That(alias1 == alias3).IsTrue();
        await Assert.That(alias1 == alias4).IsTrue();
        await Assert.That(alias2 == alias3).IsTrue();
        await Assert.That(alias2 == alias4).IsTrue();
        await Assert.That(alias3 == alias4).IsTrue();
        await Assert.That(alias1 != alias2).IsFalse();
        await Assert.That(alias1 != alias3).IsFalse();
        await Assert.That(alias1 != alias4).IsFalse();
        await Assert.That(alias2 != alias3).IsFalse();
        await Assert.That(alias2 != alias4).IsFalse();
        await Assert.That(alias3 != alias4).IsFalse();
        await Assert.That(alias1.Equals(alias2)).IsTrue();
        await Assert.That(alias1.Equals(alias3)).IsTrue();
        await Assert.That(alias1.Equals(alias4)).IsTrue();
        await Assert.That(alias2.Equals(alias3)).IsTrue();
        await Assert.That(alias2.Equals(alias4)).IsTrue();
        await Assert.That(alias3.Equals(alias4)).IsTrue();
    }

    [Test]
    public async Task Equivalent_EvmAddress_EntityIds_Are_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        var evmAddress2 = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        await Assert.That(evmAddress1).IsEqualTo(evmAddress2);
        await Assert.That(evmAddress1 == evmAddress2).IsTrue();
        await Assert.That(evmAddress1 != evmAddress2).IsFalse();
        await Assert.That(evmAddress1.Equals(evmAddress2)).IsTrue();
        await Assert.That(evmAddress2.Equals(evmAddress1)).IsTrue();
        await Assert.That(null as EvmAddress == null as EvmAddress).IsTrue();
        await Assert.That(EvmAddress.None.Equals(EvmAddress.None)).IsTrue();
    }

    [Test]
    public async Task Disimilar_EvmAddress_EntityIds_Are_Not_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var bytes1 = Generator.KeyPair().publicKey[^20..];
        var bytes2 = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EntityId(shardNum, realmNum, new EvmAddress(bytes1));
        await Assert.That(evmAddress1).IsNotEqualTo(new EntityId(shardNum, realmNum + 1, new EvmAddress(bytes1)));
        await Assert.That(evmAddress1).IsNotEqualTo(new EntityId(shardNum + 1, realmNum, new EvmAddress(bytes1)));
        await Assert.That(evmAddress1).IsNotEqualTo(new EntityId(shardNum, realmNum, new EvmAddress(bytes2)));
        await Assert.That(evmAddress1 == new EntityId(shardNum, realmNum, new EvmAddress(bytes2))).IsFalse();
        await Assert.That(evmAddress1 != new EntityId(shardNum, realmNum, new EvmAddress(bytes2))).IsTrue();
        await Assert.That(evmAddress1.Equals(new EntityId(shardNum + 1, realmNum, new EvmAddress(bytes1)))).IsFalse();
        await Assert.That(evmAddress1.Equals(new EntityId(shardNum, realmNum + 1, new EvmAddress(bytes1)))).IsFalse();
        await Assert.That(evmAddress1.Equals(new EntityId(shardNum, realmNum, new EvmAddress(bytes2)))).IsFalse();
    }

    [Test]
    public async Task Null_EvmAddress_EntityIds_Are_Not_Considered_Equal()
    {
        object asNull = null;
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmEntityId = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        await Assert.That(evmEntityId == null).IsFalse();
        await Assert.That(null == evmEntityId).IsFalse();
        await Assert.That(evmEntityId != null).IsTrue();
        await Assert.That(evmEntityId.Equals(null)).IsFalse();
        await Assert.That(evmEntityId.Equals(asNull)).IsFalse();
        await Assert.That(evmEntityId.Equals(EvmAddress.None)).IsFalse();
    }

    [Test]
    public async Task EvmAddress_Cast_As_Object_Is_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmEntityId = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        object equivalent = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        await Assert.That(evmEntityId.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(evmEntityId)).IsTrue();
    }

    [Test]
    public async Task EvmAddress_Reference_Equal_Is_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmEntityId = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        object reference = evmEntityId;
        await Assert.That(evmEntityId.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(evmEntityId)).IsTrue();
    }

    [Test]
    public async Task Equivalent_EvmAddress_EntityIds_With_Different_Constructors()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = (EntityId)new EvmAddress(bytes);
        var evmAddress2 = new EntityId(0, 0, new EvmAddress(bytes));
        await Assert.That(evmAddress1).IsEqualTo(evmAddress2);
        await Assert.That(evmAddress1 == evmAddress2).IsTrue();
        await Assert.That(evmAddress1 != evmAddress2).IsFalse();
        await Assert.That(evmAddress1.Equals(evmAddress2)).IsTrue();
    }

    [Test]
    public async Task Different_EntityId_Types_Are_Not_Considered_Equal()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        var evmEntityId = new EntityId(shardNum, realmNum, new EvmAddress(Generator.KeyPair().publicKey[^20..]));
        var alias = new EntityId(shardNum, realmNum, new Endorsement(Generator.KeyPair().publicKey));

        await Assert.That(entityId == evmEntityId).IsFalse();
        await Assert.That(evmEntityId == alias).IsFalse();
        await Assert.That(alias == entityId).IsFalse();
        await Assert.That(entityId != evmEntityId).IsTrue();
        await Assert.That(evmEntityId != alias).IsTrue();
        await Assert.That(alias != entityId).IsTrue();

        await Assert.That(entityId.ShardNum).IsEqualTo(shardNum);
        await Assert.That(alias.ShardNum).IsEqualTo(shardNum);
        await Assert.That(evmEntityId.ShardNum).IsEqualTo(shardNum);
        await Assert.That(entityId.RealmNum).IsEqualTo(realmNum);
        await Assert.That(alias.RealmNum).IsEqualTo(realmNum);
        await Assert.That(evmEntityId.RealmNum).IsEqualTo(realmNum);
    }

    [Test]
    public async Task Can_Extract_EvmAddress_From_EntityId()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var bytes = Generator.KeyPair().publicKey[^20..];
        var entityId = new EntityId(shardNum, realmNum, new EvmAddress(bytes));
        var evmAddress1 = new EvmAddress(bytes);

        await Assert.That(entityId.TryGetEvmAddress(out EvmAddress evmAddress2)).IsTrue();
        await Assert.That(evmAddress1 == evmAddress2).IsTrue();

        await Assert.That(entityId.TryGetKeyAlias(out var alias)).IsFalse();
        await Assert.That(alias).IsNull();
    }

    [Test]
    public async Task Can_Extract_Alias_From_EntityId()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var (publicKey, _) = Generator.KeyPair();
        var entityId = new EntityId(shardNum, realmNum, new Endorsement(publicKey));
        var alias1 = new Endorsement(publicKey);

        await Assert.That(entityId.TryGetKeyAlias(out var alias2)).IsTrue();
        await Assert.That(alias1 == alias2).IsTrue();

        await Assert.That(entityId.TryGetEvmAddress(out EvmAddress evmAddress)).IsFalse();
        await Assert.That(evmAddress).IsNull();
    }

    [Test]
    public async Task AccountNum_Property_Is_Set_Correctly()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId.AccountNum).IsEqualTo(accountNum);
    }

    [Test]
    public async Task IsShardRealmNum_Returns_True_For_Standard_EntityId()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 200));
        await Assert.That(entityId.IsShardRealmNum).IsTrue();
        await Assert.That(entityId.IsEvmAddress).IsFalse();
        await Assert.That(entityId.IsKeyAlias).IsFalse();
    }

    [Test]
    public async Task IsEvmAddress_Returns_True_For_EvmAddress_EntityId()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var entityId = new EntityId(0, 0, new EvmAddress(bytes));
        await Assert.That(entityId.IsEvmAddress).IsTrue();
        await Assert.That(entityId.IsShardRealmNum).IsFalse();
        await Assert.That(entityId.IsKeyAlias).IsFalse();
    }

    [Test]
    public async Task IsKeyAlias_Returns_True_For_Alias_EntityId()
    {
        var (publicKey, _) = Generator.KeyPair();
        var entityId = new EntityId(0, 0, new Endorsement(publicKey));
        await Assert.That(entityId.IsKeyAlias).IsTrue();
        await Assert.That(entityId.IsShardRealmNum).IsFalse();
        await Assert.That(entityId.IsEvmAddress).IsFalse();
    }

    [Test]
    public async Task EntityId_None_Equals_Zero_Zero_Zero()
    {
        var none = EntityId.None;
        var zero = new EntityId(0, 0, 0);
        await Assert.That(none).IsEqualTo(zero);
        await Assert.That(none.ShardNum).IsEqualTo(0);
        await Assert.That(none.RealmNum).IsEqualTo(0);
        await Assert.That(none.AccountNum).IsEqualTo(0);
        await Assert.That(none.IsShardRealmNum).IsTrue();
    }

    [Test]
    public async Task ToString_Returns_ShardRealmNum_Format()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId.ToString()).IsEqualTo($"{shardNum}.{realmNum}.{accountNum}");
    }

    [Test]
    public async Task ToString_Returns_EvmAddress_Format_For_EvmAddress_EntityId()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var entityId = new EntityId(0, 0, evmAddress);
        await Assert.That(entityId.ToString()).IsEqualTo(evmAddress.ToString());
    }

    [Test]
    public async Task ToString_Returns_Endorsement_Format_For_Alias()
    {
        var (publicKey, _) = Generator.KeyPair();
        var endorsement = new Endorsement(publicKey);
        var entityId = new EntityId(0, 0, endorsement);
        await Assert.That(entityId.ToString()).IsEqualTo(endorsement.ToString());
    }

    [Test]
    public async Task Equal_EntityIds_Have_Equal_HashCodes()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var entityId1 = new EntityId(shardNum, realmNum, accountNum);
        var entityId2 = new EntityId(shardNum, realmNum, accountNum);
        await Assert.That(entityId1.GetHashCode()).IsEqualTo(entityId2.GetHashCode());
    }

    [Test]
    public async Task Equal_Alias_EntityIds_Have_Equal_HashCodes()
    {
        var (publicKey, _) = Generator.KeyPair();
        var alias1 = new EntityId(0, 0, new Endorsement(publicKey));
        var alias2 = new EntityId(0, 0, new Endorsement(publicKey));
        await Assert.That(alias1.GetHashCode()).IsEqualTo(alias2.GetHashCode());
    }

    [Test]
    public async Task Equal_EvmAddress_EntityIds_Have_Equal_HashCodes()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress1 = new EntityId(0, 0, new EvmAddress(bytes));
        var evmAddress2 = new EntityId(0, 0, new EvmAddress(bytes));
        await Assert.That(evmAddress1.GetHashCode()).IsEqualTo(evmAddress2.GetHashCode());
    }

    [Test]
    public async Task TryParseShardRealmNum_Parses_Valid_String()
    {
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var input = $"{shardNum}.{realmNum}.{accountNum}";
        await Assert.That(EntityId.TryParseShardRealmNum(input, out var entityId)).IsTrue();
        await Assert.That(entityId).IsEqualTo(new EntityId(shardNum, realmNum, accountNum));
    }

    [Test]
    public async Task TryParseShardRealmNum_Rejects_Null()
    {
        await Assert.That(EntityId.TryParseShardRealmNum(null as string, out var entityId)).IsFalse();
        await Assert.That(entityId).IsNull();
    }

    [Test]
    public async Task TryParseShardRealmNum_Rejects_Too_Short_String()
    {
        await Assert.That(EntityId.TryParseShardRealmNum("1.2", out var entityId)).IsFalse();
        await Assert.That(entityId).IsNull();
    }

    [Test]
    public async Task TryParseShardRealmNum_Rejects_Missing_Dots()
    {
        await Assert.That(EntityId.TryParseShardRealmNum("12345", out var entityId)).IsFalse();
        await Assert.That(entityId).IsNull();
    }

    [Test]
    public async Task TryParseShardRealmNum_Rejects_Non_Numeric()
    {
        await Assert.That(EntityId.TryParseShardRealmNum("a.b.c", out var entityId)).IsFalse();
        await Assert.That(entityId).IsNull();
    }

    [Test]
    public async Task TryParseShardRealmNum_Span_Parses_Valid_Input()
    {
        var input = "10.20.30".AsSpan();
        await Assert.That(EntityId.TryParseShardRealmNum(input, out var entityId)).IsTrue();
        await Assert.That(entityId).IsEqualTo(new EntityId(10, 20, 30));
    }

    [Test]
    public async Task CastToEvmAddress_From_ShardRealmNum()
    {
        var entityId = new EntityId(0, 0, 10);
        var evmAddress = entityId.CastToEvmAddress();
        await Assert.That(evmAddress).IsNotNull();
    }

    [Test]
    public async Task CastToEvmAddress_From_Existing_EvmAddress()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var original = new EvmAddress(bytes);
        var entityId = new EntityId(0, 0, original);
        var result = entityId.CastToEvmAddress();
        await Assert.That(result).IsEqualTo(original);
    }

    [Test]
    public async Task CastToEvmAddress_From_Ecdsa_Alias()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var entityId = new EntityId(0, 0, new Endorsement(publicKey));
        var evmAddress = entityId.CastToEvmAddress();
        await Assert.That(evmAddress).IsNotNull();
    }

    [Test]
    public async Task Constructor_Rejects_Negative_ShardNum()
    {
        await Assert.That(() => new EntityId(-1, 0, 0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_Rejects_Negative_RealmNum()
    {
        await Assert.That(() => new EntityId(0, -1, 0)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Constructor_Rejects_Negative_AccountNum()
    {
        await Assert.That(() => new EntityId(0, 0, -1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Alias_Constructor_Rejects_Negative_ShardNum()
    {
        var (publicKey, _) = Generator.KeyPair();
        await Assert.That(() => new EntityId(-1, 0, new Endorsement(publicKey))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Alias_Constructor_Rejects_Negative_RealmNum()
    {
        var (publicKey, _) = Generator.KeyPair();
        await Assert.That(() => new EntityId(0, -1, new Endorsement(publicKey))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Alias_Constructor_Rejects_None_Endorsement()
    {
        await Assert.That(() => new EntityId(0, 0, Endorsement.None)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Alias_Constructor_Rejects_List_Endorsement()
    {
        var (pub1, _) = Generator.KeyPair();
        var (pub2, _) = Generator.KeyPair();
        var listEndorsement = new Endorsement(new Endorsement(pub1), new Endorsement(pub2));
        await Assert.That(() => new EntityId(0, 0, listEndorsement)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EvmAddress_Constructor_Rejects_Negative_ShardNum()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        await Assert.That(() => new EntityId(-1, 0, new EvmAddress(bytes))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EvmAddress_Constructor_Rejects_Negative_RealmNum()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        await Assert.That(() => new EntityId(0, -1, new EvmAddress(bytes))).Throws<ArgumentOutOfRangeException>();
    }
}
