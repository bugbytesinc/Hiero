// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Proto;

namespace Hiero.Test.Unit.Core;

public class ConsensusNodeInfoTests
{
    [Test]
    public async Task FromAddressBook_Returns_Empty_Array_For_Empty_Address_Book()
    {
        var result = ConsensusNodeInfoExtensions.FromAddressBook(new NodeAddressBook());

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task FromAddressBook_Maps_Node_Address_Book()
    {
        var nodeAccount = new EntityId(0, 0, 3);
        var book = new NodeAddressBook();
        var node = new NodeAddress
        {
            NodeId = 7,
            RSAPubKey = "rsa-key",
            NodeAccountId = new AccountID(nodeAccount),
            NodeCertHash = ByteString.CopyFrom([0x01, 0x02, 0x03]),
            Description = "node seven"
        };
        node.ServiceEndpoint.Add(new ServiceEndpoint
        {
            IpAddressV4 = ByteString.CopyFrom([127, 0, 0, 1]),
            Port = 50211
        });
        book.NodeAddress.Add(node);

        var result = ConsensusNodeInfoExtensions.FromAddressBook(book);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(7);
        await Assert.That(result[0].RsaPublicKey).IsEqualTo("rsa-key");
        await Assert.That(result[0].Address).IsEqualTo(nodeAccount);
        await Assert.That(result[0].CertificateHash.Span.SequenceEqual(new byte[] { 0x01, 0x02, 0x03 })).IsTrue();
        await Assert.That(result[0].Description).IsEqualTo("node seven");
        await Assert.That(result[0].Endpoints.Length).IsEqualTo(1);
        await Assert.That(result[0].Endpoints[0].IpAddress.Span.SequenceEqual(new byte[] { 127, 0, 0, 1 })).IsTrue();
        await Assert.That(result[0].Endpoints[0].Port).IsEqualTo(50211);
    }
}
