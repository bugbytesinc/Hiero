// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.Token;

public class AirdropTests
{
    [Test]
    public async Task Fungible_Constructor_Maps_Properties_And_Nft_Is_Null()
    {
        var sender = new EntityId(0, 0, Generator.Integer(10, 200));
        var receiver = new EntityId(0, 0, Generator.Integer(200, 400));
        var fungibleToken = new EntityId(0, 0, Generator.Integer(400, 600));
        var airdrop = new Airdrop(fungibleToken, sender, receiver);
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
        var airdrop = new Airdrop(nft, sender, receiver);
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
        var a1 = new Airdrop(fungibleToken, sender, receiver);
        var a2 = new Airdrop(fungibleToken, sender, receiver);
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
        var a1 = new Airdrop(fungibleToken, sender, receiver1);
        var a2 = new Airdrop(fungibleToken, sender, receiver2);
        await Assert.That(a1).IsNotEqualTo(a2);
        await Assert.That(a1 == a2).IsFalse();
    }

    [Test]
    public async Task AirdropAmount_Fungible_Maps_Token_Sender_Receiver_And_Amount()
    {
        // Regression test: the projection from a pending-airdrop record must map
        // token/sender/receiver to the correct Airdrop fields (they were previously
        // cross-wired by a scrambled positional constructor call).
        var senderNum = Generator.Integer(10, 200);
        var receiverNum = Generator.Integer(200, 400);
        var tokenNum = Generator.Integer(400, 600);
        var amount = (ulong)Generator.Integer(1, 100_000);
        var record = new PendingAirdropRecord
        {
            PendingAirdropId = new PendingAirdropId
            {
                SenderId = new AccountID { AccountNum = senderNum },
                ReceiverId = new AccountID { AccountNum = receiverNum },
                FungibleTokenType = new TokenID { TokenNum = tokenNum },
            },
            PendingAirdropValue = new PendingAirdropValue { Amount = amount },
        };
        var airdropAmount = new AirdropAmount(record);
        await Assert.That(airdropAmount.Airdrop.Token).IsEqualTo(new EntityId(0, 0, tokenNum));
        await Assert.That(airdropAmount.Airdrop.Sender).IsEqualTo(new EntityId(0, 0, senderNum));
        await Assert.That(airdropAmount.Airdrop.Receiver).IsEqualTo(new EntityId(0, 0, receiverNum));
        await Assert.That(airdropAmount.Airdrop.Nft).IsNull();
        await Assert.That(airdropAmount.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task AirdropAmount_Nft_Maps_Nft_Sender_Receiver_And_Amount()
    {
        // Regression test: the NFT projection previously bound to the all-EntityId
        // constructor via the implicit Nft->EntityId operator, dropping the serial
        // number entirely. The Nft (with serial) must round-trip intact.
        var senderNum = Generator.Integer(10, 200);
        var receiverNum = Generator.Integer(200, 400);
        var tokenNum = Generator.Integer(400, 600);
        long serial = Generator.Integer(1, 100);
        var record = new PendingAirdropRecord
        {
            PendingAirdropId = new PendingAirdropId
            {
                SenderId = new AccountID { AccountNum = senderNum },
                ReceiverId = new AccountID { AccountNum = receiverNum },
                NonFungibleToken = new NftID
                {
                    TokenID = new TokenID { TokenNum = tokenNum },
                    SerialNumber = serial,
                },
            },
        };
        var airdropAmount = new AirdropAmount(record);
        await Assert.That(airdropAmount.Airdrop.Nft).IsEqualTo(new Hiero.Nft(new EntityId(0, 0, tokenNum), serial));
        await Assert.That(airdropAmount.Airdrop.Sender).IsEqualTo(new EntityId(0, 0, senderNum));
        await Assert.That(airdropAmount.Airdrop.Receiver).IsEqualTo(new EntityId(0, 0, receiverNum));
        await Assert.That(airdropAmount.Airdrop.Token).IsNull();
        await Assert.That(airdropAmount.Amount).IsEqualTo((ulong)1);
    }
}
