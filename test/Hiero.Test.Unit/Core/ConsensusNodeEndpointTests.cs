// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Null arguments are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class ConsensusNodeEndpointTests
{
    [Test]
    public async Task Equivalent_Endpoints_Are_Considered_Equal()
    {
        var uri = new Uri("http://testnet.hedera.com:123");
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var endpoint1 = new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum, accountNum), uri);
        var endpoint2 = new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum, accountNum), uri);
        var endpoint3 = new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum, accountNum), new Uri("http://testnet.hedera.com:123"));
        await Assert.That(endpoint1).IsEqualTo(endpoint2);
        await Assert.That(endpoint1).IsEqualTo(endpoint3);
        await Assert.That(endpoint2).IsEqualTo(endpoint3);
        await Assert.That(endpoint1 == endpoint2).IsTrue();
        await Assert.That(endpoint2 == endpoint3).IsTrue();
        await Assert.That(endpoint1 == endpoint3).IsTrue();
        await Assert.That(endpoint1 != endpoint2).IsFalse();
        await Assert.That(endpoint1 != endpoint3).IsFalse();
        await Assert.That(endpoint2 != endpoint3).IsFalse();
    }

    [Test]
    public async Task Disimilar_Endpoints_Are_Not_Considered_Equal()
    {
        var uri = new Uri("http://testnet.hedera.com:123");
        var shardNum = Generator.Integer(0, 200);
        var realmNum = Generator.Integer(0, 200);
        var accountNum = Generator.Integer(0, 200);
        var endpoint1 = new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum, accountNum), uri);
        await Assert.That(endpoint1).IsNotEqualTo(new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum, accountNum), new Uri("http://testnet.hedera.com:1234")));
        await Assert.That(endpoint1).IsNotEqualTo(new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum + 1, accountNum), uri));
        await Assert.That(endpoint1).IsNotEqualTo(new ConsensusNodeEndpoint(new EntityId(shardNum + 1, realmNum, accountNum), uri));
        await Assert.That(endpoint1).IsNotEqualTo(new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum, accountNum + 1), uri));
        await Assert.That(endpoint1 == new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum + 1, accountNum), uri)).IsFalse();
        await Assert.That(endpoint1 != new ConsensusNodeEndpoint(new EntityId(shardNum, realmNum + 1, accountNum), uri)).IsTrue();
    }

    [Test]
    public async Task Equal_Endpoints_Have_Equal_HashCodes()
    {
        var uri = new Uri("http://testnet.hedera.com:123");
        var node = new EntityId(0, 0, Generator.Integer(3, 100));
        var endpoint1 = new ConsensusNodeEndpoint(node, uri);
        var endpoint2 = new ConsensusNodeEndpoint(new EntityId(node.ShardNum, node.RealmNum, node.AccountNum), new Uri("http://testnet.hedera.com:123"));
        await Assert.That(endpoint1.GetHashCode()).IsEqualTo(endpoint2.GetHashCode());
    }

    [Test]
    public async Task Properties_Are_Mapped_Correctly()
    {
        var uri = new Uri("http://testnet.hedera.com:50211");
        var node = new EntityId(0, 0, 3);
        var endpoint = new ConsensusNodeEndpoint(node, uri);
        await Assert.That(endpoint.Uri).IsEqualTo(uri);
        await Assert.That(endpoint.Node).IsEqualTo(node);
    }

    [Test]
    public async Task Null_Uri_Throws_Error()
    {
        var node = new EntityId(0, 0, 3);
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            new ConsensusNodeEndpoint(node, null);
        });
        await Assert.That(exception.ParamName).IsEqualTo("uri");
        await Assert.That(exception.Message).StartsWith("URL is required.");
    }

    [Test]
    public async Task Null_Node_Throws_Error()
    {
        var uri = new Uri("http://testnet.hedera.com:50211");
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            new ConsensusNodeEndpoint(null, uri);
        });
        await Assert.That(exception.ParamName).IsEqualTo("node");
        await Assert.That(exception.Message).StartsWith("Node wallet address is required.");
    }

    [Test]
    public async Task None_Node_Throws_Error()
    {
        var uri = new Uri("http://testnet.hedera.com:50211");
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            new ConsensusNodeEndpoint(EntityId.None, uri);
        });
        await Assert.That(exception.ParamName).IsEqualTo("node");
        await Assert.That(exception.Message).StartsWith("Node wallet address can not be None.");
    }

    [Test]
    public async Task Non_ShardRealmNum_Node_Throws_Error()
    {
        var uri = new Uri("http://testnet.hedera.com:50211");
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var aliasNode = new EntityId(0, 0, new Endorsement(publicKey));
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new ConsensusNodeEndpoint(aliasNode, uri);
        });
        await Assert.That(exception.ParamName).IsEqualTo("node");
        await Assert.That(exception.Message).StartsWith("Node wallet address must be in the form of [shard.realm.num].");
    }

    [Test]
    public async Task Implicit_Operator_Converts_To_EntityId()
    {
        var node = new EntityId(0, 0, Generator.Integer(3, 100));
        var uri = new Uri("http://testnet.hedera.com:50211");
        var endpoint = new ConsensusNodeEndpoint(node, uri);
        EntityId entityId = endpoint;
        await Assert.That(entityId).IsEqualTo(node);
    }

    [Test]
    public async Task ToString_Contains_Node_And_Uri()
    {
        var node = new EntityId(0, 0, 3);
        var uri = new Uri("http://testnet.hedera.com:50211");
        var endpoint = new ConsensusNodeEndpoint(node, uri);
        var result = endpoint.ToString();
        await Assert.That(result).Contains("0.0.3");
        await Assert.That(result).Contains("testnet.hedera.com");
    }

    [Test]
    public async Task CertificateHash_Defaults_To_Empty()
    {
        var endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("https://testnet.hedera.com:50212"));
        await Assert.That(endpoint.CertificateHash.IsEmpty).IsTrue();
    }

    [Test]
    public async Task CertificateHash_Constructor_Maps_Value()
    {
        var hash = new byte[] { 0xea, 0xdd, 0x72, 0xfc };
        var endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("https://testnet.hedera.com:50212"), hash);
        await Assert.That(endpoint.CertificateHash.Span.SequenceEqual(hash)).IsTrue();
    }

    [Test]
    public async Task CertificateHash_Participates_In_Equality_By_Content()
    {
        // Different pinning targets are different endpoints (and therefore
        // different channel cache keys); a pinned endpoint is not equal to an
        // unpinned one to the same URI.
        var node = new EntityId(0, 0, 3);
        var uri = new Uri("https://testnet.hedera.com:50212");
        var withHash = new ConsensusNodeEndpoint(node, uri, new byte[] { 0x01, 0x02, 0x03 });
        var withoutHash = new ConsensusNodeEndpoint(node, uri);
        var withOtherHash = new ConsensusNodeEndpoint(node, uri, new byte[] { 0x09, 0x08 });
        await Assert.That(withHash).IsNotEqualTo(withoutHash);
        await Assert.That(withHash).IsNotEqualTo(withOtherHash);
        await Assert.That(withHash == withoutHash).IsFalse();

        // ...but equal hash *content* in a separate array is equal (compared by
        // value, not by backing-array reference), and yields an equal hash code.
        var sameHashDifferentArray = new ConsensusNodeEndpoint(node, uri, new byte[] { 0x01, 0x02, 0x03 });
        await Assert.That(withHash).IsEqualTo(sameHashDifferentArray);
        await Assert.That(withHash == sameHashDifferentArray).IsTrue();
        await Assert.That(withHash.GetHashCode()).IsEqualTo(sameHashDifferentArray.GetHashCode());
    }

    [Test]
    public async Task Equals_Null_Returns_False()
    {
        var endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("http://testnet.hedera.com:50211"));
        await Assert.That(endpoint.Equals(null)).IsFalse();
    }

    [Test]
    public async Task Equals_Different_Type_Returns_False()
    {
        var endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("http://testnet.hedera.com:50211"));
        await Assert.That(endpoint.Equals("not an endpoint")).IsFalse();
    }
}
