// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TokenTransferListTests
{
    [Test]
    public async Task AsTransferLists_Maps_Fungible_Transfers()
    {
        var token = new EntityId(0, 0, 2001);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var tokenTransferList = new TokenTransferList
        {
            Token = new TokenID(token)
        };
        tokenTransferList.Transfers.Add(new AccountAmount { AccountID = new AccountID(sender), Amount = -10 });
        tokenTransferList.Transfers.Add(new AccountAmount { AccountID = new AccountID(receiver), Amount = 10 });
        var lists = new RepeatedField<TokenTransferList>();
        lists.Add(tokenTransferList);

        var (tokenTransfers, nftTransfers, treasuryTransfer) = lists.AsTransferLists();

        await Assert.That(tokenTransfers.Count).IsEqualTo(2);
        await Assert.That(tokenTransfers[0]).IsEqualTo(new TokenTransfer(token, sender, -10));
        await Assert.That(tokenTransfers[1]).IsEqualTo(new TokenTransfer(token, receiver, 10));
        await Assert.That(nftTransfers).IsEmpty();
        await Assert.That(treasuryTransfer).IsNull();
    }

    [Test]
    public async Task AsTransferLists_Maps_Nft_Transfers_And_Treasury_Transfer()
    {
        var token = new EntityId(0, 0, 2001);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var oldTreasury = new EntityId(0, 0, 1003);
        var newTreasury = new EntityId(0, 0, 1004);
        var tokenTransferList = new TokenTransferList
        {
            Token = new TokenID(token)
        };
        tokenTransferList.NftTransfers.Add(new Proto.NftTransfer
        {
            SenderAccountID = new AccountID(sender),
            ReceiverAccountID = new AccountID(receiver),
            SerialNumber = 1
        });
        tokenTransferList.NftTransfers.Add(new Proto.NftTransfer
        {
            SenderAccountID = new AccountID(oldTreasury),
            ReceiverAccountID = new AccountID(newTreasury),
            SerialNumber = -1
        });
        var lists = new RepeatedField<TokenTransferList>();
        lists.Add(tokenTransferList);

        var (tokenTransfers, nftTransfers, treasuryTransfer) = lists.AsTransferLists();

        await Assert.That(tokenTransfers).IsEmpty();
        await Assert.That(nftTransfers.Count).IsEqualTo(1);
        await Assert.That(nftTransfers[0]).IsEqualTo(new Hiero.NftTransfer(new Hiero.Nft(token, 1), sender, receiver));
        await Assert.That(treasuryTransfer).IsEqualTo(new TreasuryTransfer(token, oldTreasury, newTreasury));
    }
}
