// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Nft;

public class NftTransferTests
{
    [Test]
    public async Task Constructor_Maps_All_Properties()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        long serial = Generator.Integer(1, 100);
        var nft = new Hiero.Nft(token, serial);
        var from = new EntityId(0, 0, Generator.Integer(200, 400));
        var to = new EntityId(0, 0, Generator.Integer(400, 600));
        var senderHook = new HookCall(1, new byte[] { 0x01 }, 100);
        var receiverHook = new HookCall(2, new byte[] { 0x02 }, 200);
        var transfer = new NftTransfer(nft, from, to, true, senderHook, receiverHook);
        await Assert.That(transfer.Nft).IsEqualTo(nft);
        await Assert.That(transfer.Sender).IsEqualTo(from);
        await Assert.That(transfer.Receiver).IsEqualTo(to);
        await Assert.That(transfer.Delegated).IsTrue();
        await Assert.That(transfer.SenderAllowanceHook).IsEqualTo(senderHook);
        await Assert.That(transfer.ReceiverAllowanceHook).IsEqualTo(receiverHook);
    }

    [Test]
    public async Task Delegated_Defaults_To_False()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var nft = new Hiero.Nft(token, Generator.Integer(1, 100));
        var from = new EntityId(0, 0, Generator.Integer(200, 400));
        var to = new EntityId(0, 0, Generator.Integer(400, 600));
        var transfer = new NftTransfer(nft, from, to);
        await Assert.That(transfer.Delegated).IsFalse();
    }

    [Test]
    public async Task Hooks_Default_To_Null()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var nft = new Hiero.Nft(token, Generator.Integer(1, 100));
        var from = new EntityId(0, 0, Generator.Integer(200, 400));
        var to = new EntityId(0, 0, Generator.Integer(400, 600));
        var transfer = new NftTransfer(nft, from, to);
        await Assert.That(transfer.SenderAllowanceHook).IsNull();
        await Assert.That(transfer.ReceiverAllowanceHook).IsNull();
    }

    [Test]
    public async Task With_Hooks_Provided_Maps_Correctly()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var nft = new Hiero.Nft(token, Generator.Integer(1, 100));
        var from = new EntityId(0, 0, Generator.Integer(200, 400));
        var to = new EntityId(0, 0, Generator.Integer(400, 600));
        var senderHook = new HookCall(1, new byte[] { 0x01 }, 100);
        var receiverHook = new HookCall(2, new byte[] { 0x02 }, 200);
        var transfer = new NftTransfer(nft, from, to, senderAllowanceHook: senderHook, receiverAllowanceHook: receiverHook);
        await Assert.That(transfer.SenderAllowanceHook).IsEqualTo(senderHook);
        await Assert.That(transfer.ReceiverAllowanceHook).IsEqualTo(receiverHook);
    }

    [Test]
    public async Task Equivalent_Transfers_Are_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        long serial = Generator.Integer(1, 100);
        var nft = new Hiero.Nft(token, serial);
        var from = new EntityId(0, 0, Generator.Integer(200, 400));
        var to = new EntityId(0, 0, Generator.Integer(400, 600));
        var t1 = new NftTransfer(nft, from, to);
        var t2 = new NftTransfer(nft, from, to);
        await Assert.That(t1).IsEqualTo(t2);
        await Assert.That(t1 == t2).IsTrue();
    }

    [Test]
    public async Task Different_Transfers_Are_Not_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(10, 200));
        var nft1 = new Hiero.Nft(token, Generator.Integer(1, 50));
        var nft2 = new Hiero.Nft(token, Generator.Integer(51, 100));
        var from = new EntityId(0, 0, Generator.Integer(200, 400));
        var to = new EntityId(0, 0, Generator.Integer(400, 600));
        var t1 = new NftTransfer(nft1, from, to);
        var t2 = new NftTransfer(nft2, from, to);
        await Assert.That(t1).IsNotEqualTo(t2);
        await Assert.That(t1 == t2).IsFalse();
    }
}
