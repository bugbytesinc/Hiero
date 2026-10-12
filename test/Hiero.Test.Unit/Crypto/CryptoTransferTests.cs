// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Crypto;

public class CryptoTransferTests
{
    [Test]
    public async Task Constructor_Maps_Properties_Correctly()
    {
        var address = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var hook = new HookCall(1, new byte[] { 0x01 }, 100);
        var ct = new CryptoTransfer(address, amount, true, hook);
        await Assert.That(ct.Address).IsEqualTo(address);
        await Assert.That(ct.Amount).IsEqualTo(amount);
        await Assert.That(ct.Delegated).IsTrue();
        await Assert.That(ct.AllowanceHook).IsEqualTo(hook);
    }

    [Test]
    public async Task Delegated_Defaults_To_False()
    {
        var address = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var ct = new CryptoTransfer(address, amount);
        await Assert.That(ct.Delegated).IsFalse();
    }

    [Test]
    public async Task AllowanceHook_Defaults_To_Null()
    {
        var address = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var ct = new CryptoTransfer(address, amount);
        await Assert.That(ct.AllowanceHook).IsNull();
    }

    [Test]
    public async Task Delegated_True_Maps_Correctly()
    {
        var address = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var ct = new CryptoTransfer(address, amount, delegated: true);
        await Assert.That(ct.Delegated).IsTrue();
    }

    [Test]
    public async Task AllowanceHook_Maps_Correctly_When_Provided()
    {
        var address = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var hook = new HookCall(42, new byte[] { 0xAB, 0xCD }, 50_000);
        var ct = new CryptoTransfer(address, amount, allowanceHook: hook);
        await Assert.That(ct.AllowanceHook).IsEqualTo(hook);
    }

    [Test]
    public async Task Equivalent_Transfers_Are_Equal()
    {
        var address = new EntityId(0, 0, Generator.Integer(10, 200));
        long amount = Generator.Integer(500, 1000);
        var ct1 = new CryptoTransfer(address, amount);
        var ct2 = new CryptoTransfer(address, amount);
        await Assert.That(ct1).IsEqualTo(ct2);
        await Assert.That(ct1 == ct2).IsTrue();
        await Assert.That(ct1 != ct2).IsFalse();
    }

    [Test]
    public async Task Different_Transfers_Are_Not_Equal()
    {
        var address1 = new EntityId(0, 0, Generator.Integer(10, 200));
        var address2 = new EntityId(0, 0, Generator.Integer(300, 500));
        long amount = Generator.Integer(500, 1000);
        var ct1 = new CryptoTransfer(address1, amount);
        var ct2 = new CryptoTransfer(address2, amount);
        await Assert.That(ct1).IsNotEqualTo(ct2);
        await Assert.That(ct1 == ct2).IsFalse();
        await Assert.That(ct1 != ct2).IsTrue();
    }
}
