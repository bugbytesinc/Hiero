// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Token;

public class AirdropTests
{
    [Test]
    public async Task Fungible_Constructor_Maps_Properties_And_Nft_Is_Null()
    {
        var sender = new EntityId(0, 0, Generator.Integer(10, 200));
        var receiver = new EntityId(0, 0, Generator.Integer(200, 400));
        var fungibleToken = new EntityId(0, 0, Generator.Integer(400, 600));
        var airdrop = new Airdrop(sender, receiver, fungibleToken);
        await Assert.That(airdrop.Sender).IsEqualTo(sender);
        await Assert.That(airdrop.Receiver).IsEqualTo(receiver);
        await Assert.That(airdrop.Token).IsEqualTo(fungibleToken);
        await Assert.That(airdrop.Nft).IsNull();
    }

    [Test]
    public async Task Nft_Constructor_Maps_Properties_And_Token_Is_Null()
    {
        var sender = new EntityId(0, 0, Generator.Integer(10, 200));
        var receiver = new EntityId(0, 0, Generator.Integer(200, 400));
        var token = new EntityId(0, 0, Generator.Integer(400, 600));
        long serial = Generator.Integer(1, 100);
        var nft = new Hiero.Nft(token, serial);
        var airdrop = new Airdrop(sender, receiver, nft);
        await Assert.That(airdrop.Sender).IsEqualTo(sender);
        await Assert.That(airdrop.Receiver).IsEqualTo(receiver);
        await Assert.That(airdrop.Nft).IsEqualTo(nft);
        await Assert.That(airdrop.Token).IsNull();
    }

    [Test]
    public async Task Equivalent_Airdrops_Are_Equal()
    {
        var sender = new EntityId(0, 0, Generator.Integer(10, 200));
        var receiver = new EntityId(0, 0, Generator.Integer(200, 400));
        var fungibleToken = new EntityId(0, 0, Generator.Integer(400, 600));
        var a1 = new Airdrop(sender, receiver, fungibleToken);
        var a2 = new Airdrop(sender, receiver, fungibleToken);
        await Assert.That(a1).IsEqualTo(a2);
        await Assert.That(a1 == a2).IsTrue();
    }

    [Test]
    public async Task Different_Airdrops_Are_Not_Equal()
    {
        var sender = new EntityId(0, 0, Generator.Integer(10, 200));
        var receiver1 = new EntityId(0, 0, Generator.Integer(200, 400));
        var receiver2 = new EntityId(0, 0, Generator.Integer(400, 600));
        var fungibleToken = new EntityId(0, 0, Generator.Integer(600, 800));
        var a1 = new Airdrop(sender, receiver1, fungibleToken);
        var a2 = new Airdrop(sender, receiver2, fungibleToken);
        await Assert.That(a1).IsNotEqualTo(a2);
        await Assert.That(a1 == a2).IsFalse();
    }
}
