// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8604 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Token;

public class TokenTransferTests
{
    [Test]
    public async Task Can_Create_Token_Transfer_Object()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        await Assert.That(tt.Token).IsEqualTo(token);
        await Assert.That(tt.Account).IsEqualTo(address);
        await Assert.That(tt.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task Equivalent_TokenTransfers_Are_Considered_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt1 = new TokenTransfer(token, address, amount);
        var tt2 = new TokenTransfer(token, address, amount);
        await Assert.That(tt1).IsEqualTo(tt2);
        await Assert.That(tt1 == tt2).IsTrue();
        await Assert.That(tt1 != tt2).IsFalse();
        await Assert.That(tt1.Equals(tt2)).IsTrue();
        await Assert.That(tt2.Equals(tt1)).IsTrue();
        await Assert.That(null as TokenTransfer == null as TokenTransfer).IsTrue();
    }

    [Test]
    public async Task Disimilar_TokenTransfers_Are_Not_Considered_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        var other = new EntityId(0, 0, Generator.Integer(500, 600));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        await Assert.That(tt).IsNotEqualTo(new TokenTransfer(token, other, amount));
        await Assert.That(tt).IsNotEqualTo(new TokenTransfer(other, address, amount));
        await Assert.That(tt).IsNotEqualTo(new TokenTransfer(token, address, amount + 1));
        await Assert.That(tt == new TokenTransfer(token, address, amount + 1)).IsFalse();
        await Assert.That(tt != new TokenTransfer(token, address, amount + 1)).IsTrue();
        await Assert.That(tt.Equals(new TokenTransfer(other, address, amount))).IsFalse();
        await Assert.That(tt.Equals(new TokenTransfer(token, other, amount))).IsFalse();
        await Assert.That(tt.Equals(new TokenTransfer(token, address, amount + 1))).IsFalse();
    }

    [Test]
    public async Task Comparing_With_Null_Is_Not_Considered_Equal()
    {
        object asNull = null;
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        await Assert.That(tt == null).IsFalse();
        await Assert.That(null == tt).IsFalse();
        await Assert.That(tt != null).IsTrue();
        await Assert.That(tt!.Equals(null as TokenTransfer)).IsFalse();
        await Assert.That(tt.Equals(asNull)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        await Assert.That(tt.Equals("Something that is not a TokenTransfer")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        object equivalent = new TokenTransfer(token, address, amount);
        await Assert.That(tt.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(tt)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        object reference = tt;
        await Assert.That(tt.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(tt)).IsTrue();
    }

    [Test]
    public async Task Can_Use_With_Expression_To_Modify_Amount()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt1 = new TokenTransfer(token, address, amount);
        var tt2 = tt1 with { Amount = tt1.Amount + amount };
        var tt3 = tt2 with { Amount = tt2.Amount - amount };
        await Assert.That(tt1.Amount).IsEqualTo(amount);
        await Assert.That(tt2.Amount).IsEqualTo(amount * 2);
        await Assert.That(tt3.Amount).IsEqualTo(amount);
        await Assert.That(tt1.Equals(tt3)).IsTrue();
        await Assert.That(tt3.Equals(tt1)).IsTrue();
    }

    // --- Gap coverage tests ---

    [Test]
    public async Task Equal_TokenTransfers_Have_Equal_HashCodes()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt1 = new TokenTransfer(token, address, amount);
        var tt2 = new TokenTransfer(token, address, amount);
        await Assert.That(tt1.GetHashCode()).IsEqualTo(tt2.GetHashCode());
    }

    [Test]
    public async Task Constructor_With_Delegated_Maps_Correctly()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount, delegated: true);
        await Assert.That(tt.Delegated).IsTrue();
        await Assert.That(tt.Token).IsEqualTo(token);
        await Assert.That(tt.Account).IsEqualTo(address);
        await Assert.That(tt.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task Delegated_Default_Is_False()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        await Assert.That(tt.Delegated).IsFalse();
    }

    [Test]
    public async Task AllowanceHook_Default_Is_Null()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt = new TokenTransfer(token, address, amount);
        await Assert.That(tt.AllowanceHook).IsNull();
    }

    [Test]
    public async Task Constructor_With_AllowanceHook_Maps_Correctly()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var hook = new HookCall(1, new byte[] { 0x01, 0x02 }, 100_000);
        var tt = new TokenTransfer(token, address, amount, allowanceHook: hook);
        await Assert.That(tt.AllowanceHook).IsEqualTo(hook);
    }

    [Test]
    public async Task Different_Delegated_Values_Are_Not_Equal()
    {
        var token = new EntityId(0, 0, Generator.Integer(0, 200));
        var address = new EntityId(0, 0, Generator.Integer(200, 400));
        long amount = Generator.Integer(500, 600);
        var tt1 = new TokenTransfer(token, address, amount, delegated: false);
        var tt2 = new TokenTransfer(token, address, amount, delegated: true);
        await Assert.That(tt1).IsNotEqualTo(tt2);
    }
}
