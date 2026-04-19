// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602, CS8604 // Null assignments and dereferences are intentional in these tests
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TransactionIdTests
{
    [Test]
    public async Task Equivalent_TransactionIds_Are_Considered_Equal()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId1 = new TransactionId(payer, seconds, nanos);
        var txId2 = new TransactionId(payer, seconds, nanos);
        await Assert.That(txId1).IsEqualTo(txId2);
        await Assert.That(txId1 == txId2).IsTrue();
        await Assert.That(txId1 != txId2).IsFalse();
        await Assert.That(txId1.GetHashCode()).IsEqualTo(txId2.GetHashCode());
    }

    [Test]
    public async Task Equivalent_TransactionIds_With_Nonces_Are_Considered_Equal()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var nonce = Generator.Integer(10, 20);
        var txId1 = new TransactionId(payer, seconds, nanos, false, nonce);
        var txId2 = new TransactionId(payer, seconds, nanos, false, nonce);
        await Assert.That(txId1).IsEqualTo(txId2);
        await Assert.That(txId1 == txId2).IsTrue();
        await Assert.That(txId1 != txId2).IsFalse();
        await Assert.That(txId1.GetHashCode()).IsEqualTo(txId2.GetHashCode());
    }

    [Test]
    public async Task Disimilar_TransactionIds_Are_Not_Considered_Equal()
    {
        var payer1 = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds1, nanos1) = Epoch.UniqueSecondsAndNanos(false);
        var payer2 = new EntityId(0, 0, Generator.Integer(201, 400));
        var (seconds2, nanos2) = Epoch.UniqueSecondsAndNanos(false);
        var txId1 = new TransactionId(payer1, seconds1, nanos1);
        var txId2 = new TransactionId(payer2, seconds2, nanos2);
        await Assert.That(txId1).IsNotEqualTo(txId2);
        await Assert.That(txId1 == txId2).IsFalse();
        await Assert.That(txId1 != txId2).IsTrue();
        await Assert.That(txId1.GetHashCode()).IsNotEqualTo(txId2.GetHashCode());
    }

    [Test]
    public async Task TransactionIds_With_Disimilar_Nonces_Are_Not_Considered_Equal()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId1 = new TransactionId(payer, seconds, nanos, false, Generator.Integer(10, 20));
        var txId2 = new TransactionId(payer, seconds, nanos, false, Generator.Integer(30, 40));
        await Assert.That(txId1).IsNotEqualTo(txId2);
        await Assert.That(txId1 == txId2).IsFalse();
        await Assert.That(txId1 != txId2).IsTrue();
        await Assert.That(txId1.GetHashCode()).IsNotEqualTo(txId2.GetHashCode());
    }

    [Test]
    public async Task Payer_Property_Maps_Correctly()
    {
        var payer = new EntityId(Generator.Integer(0, 10), Generator.Integer(0, 10), Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId = new TransactionId(payer, seconds, nanos);
        await Assert.That(txId.Payer).IsEqualTo(payer);
    }

    [Test]
    public async Task ValidStartSeconds_Property_Maps_Correctly()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId = new TransactionId(payer, seconds, nanos);
        await Assert.That(txId.ValidStartSeconds).IsEqualTo(seconds);
    }

    [Test]
    public async Task ValidStartNanos_Property_Maps_Correctly()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId = new TransactionId(payer, seconds, nanos);
        await Assert.That(txId.ValidStartNanos).IsEqualTo(nanos);
    }

    [Test]
    public async Task None_Has_Zero_Values()
    {
        var empty = TransactionId.None;
        await Assert.That(empty.Payer).IsEqualTo(EntityId.None);
        await Assert.That(empty.Payer).IsEqualTo(new EntityId(0, 0, 0));
        var expectedSeconds = 0L;
        var expectedNanos = 0;
        await Assert.That(empty.ValidStartSeconds).IsEqualTo(expectedSeconds);
        await Assert.That(empty.ValidStartNanos).IsEqualTo(expectedNanos);
        await Assert.That(empty.Scheduled).IsFalse();
        var expectedNonce = 0;
        await Assert.That(empty.ChildNonce).IsEqualTo(expectedNonce);
    }

    [Test]
    public async Task Can_Create_TransactionId_With_Seconds_And_Nanos()
    {
        var address = new EntityId(Generator.Integer(0, 10), Generator.Integer(0, 10), Generator.Integer(10, 20));
        var totalNanos = Epoch.UniqueClockNanosAfterDrift();
        var seconds = totalNanos / 1_000_000_000;
        var nanos = (int)(totalNanos % 1_000_000_000);

        var txId = new TransactionId(address, seconds, nanos);

        await Assert.That(txId.Payer).IsEqualTo(address);
        await Assert.That(txId.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(txId.ValidStartNanos).IsEqualTo(nanos);
        await Assert.That(txId.Scheduled).IsFalse();
        var expectedNonce = 0;
        await Assert.That(txId.ChildNonce).IsEqualTo(expectedNonce);
    }

    [Test]
    public async Task Can_Create_TransactionId_With_DateTime()
    {
        var address = new EntityId(Generator.Integer(0, 10), Generator.Integer(0, 10), Generator.Integer(10, 20));
        var dateTime = DateTime.UtcNow;
        var (seconds, nanos) = Epoch.FromDate(dateTime);

        var txId = new TransactionId(address, dateTime);

        await Assert.That(txId.Payer).IsEqualTo(address);
        await Assert.That(txId.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(txId.ValidStartNanos).IsEqualTo(nanos);
        await Assert.That(txId.Scheduled).IsFalse();
        var expectedNonce = 0;
        await Assert.That(txId.ChildNonce).IsEqualTo(expectedNonce);
    }

    [Test]
    public async Task Can_Create_TransactionId_With_Nonce()
    {
        var address = new EntityId(Generator.Integer(0, 10), Generator.Integer(0, 10), Generator.Integer(10, 20));
        var dateTime = DateTime.UtcNow;
        var (seconds, nanos) = Epoch.FromDate(dateTime);
        var nonce = Generator.Integer(5, 20);

        var txId = new TransactionId(address, dateTime, false, nonce);

        await Assert.That(txId.Payer).IsEqualTo(address);
        await Assert.That(txId.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(txId.ValidStartNanos).IsEqualTo(nanos);
        await Assert.That(txId.Scheduled).IsFalse();
        await Assert.That(txId.ChildNonce).IsEqualTo(nonce);
    }

    [Test]
    public async Task Can_Create_Scheduled_TransactionId()
    {
        var address = new EntityId(Generator.Integer(0, 10), Generator.Integer(0, 10), Generator.Integer(10, 20));
        var dateTime = DateTime.UtcNow;
        var (seconds, nanos) = Epoch.FromDate(dateTime);
        var nonce = Generator.Integer(5, 20);

        var txId = new TransactionId(address, dateTime, true, nonce);

        await Assert.That(txId.Payer).IsEqualTo(address);
        await Assert.That(txId.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(txId.ValidStartNanos).IsEqualTo(nanos);
        await Assert.That(txId.Scheduled).IsTrue();
        await Assert.That(txId.ChildNonce).IsEqualTo(nonce);
    }

    // --- Gap coverage tests ---

    [Test]
    public async Task Comparing_With_Null_Is_Not_Considered_Equal()
    {
        object asNull = null;
        var txId = new TransactionId(new EntityId(0, 0, 5), 100, 200);
        await Assert.That(txId == null).IsFalse();
        await Assert.That(null == txId).IsFalse();
        await Assert.That(txId != null).IsTrue();
        await Assert.That(txId.Equals(null as TransactionId)).IsFalse();
        await Assert.That(txId.Equals(asNull)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), 100, 200);
        await Assert.That(txId.Equals("Something that is not a TransactionId")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId = new TransactionId(payer, seconds, nanos);
        object equivalent = new TransactionId(payer, seconds, nanos);
        await Assert.That(txId.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(txId)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), 100, 200);
        object reference = txId;
        await Assert.That(txId.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(txId)).IsTrue();
    }

    [Test]
    public async Task ToString_Basic_Format()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), 1234567890, 123456789);
        var result = txId.ToString();
        await Assert.That(result).Contains("0.0.5");
        await Assert.That(result).Contains("1234567890");
        await Assert.That(result).Contains("123456789");
    }

    [Test]
    public async Task ToString_Scheduled_Contains_Scheduled_Suffix()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), 100, 200, scheduled: true);
        var result = txId.ToString();
        await Assert.That(result).Contains("scheduled");
    }

    [Test]
    public async Task ToString_With_Nonce_Contains_Nonce()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), 100, 200, childNonce: 3);
        var result = txId.ToString();
        await Assert.That(result).Contains(":3");
    }

    [Test]
    public async Task ToString_With_Scheduled_And_Nonce()
    {
        var txId = new TransactionId(new EntityId(0, 0, 5), 100, 200, scheduled: true, childNonce: 7);
        var result = txId.ToString();
        await Assert.That(result).Contains(":7");
        await Assert.That(result).Contains("scheduled");
    }

    [Test]
    public async Task Different_Scheduled_Values_Are_Not_Equal()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var txId1 = new TransactionId(payer, seconds, nanos, scheduled: false);
        var txId2 = new TransactionId(payer, seconds, nanos, scheduled: true);
        await Assert.That(txId1).IsNotEqualTo(txId2);
    }

    [Test]
    public async Task Internal_Proto_AsTransactionId_Maps_Values()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var nonce = Generator.Integer(5, 20);
        var proto = new TransactionID
        {
            AccountID = new AccountID(payer),
            TransactionValidStart = new Timestamp { Seconds = seconds, Nanos = nanos },
            Scheduled = true,
            Nonce = nonce
        };
        var txId = proto.AsTransactionId();
        await Assert.That(txId.Payer).IsEqualTo(payer);
        await Assert.That(txId.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(txId.ValidStartNanos).IsEqualTo(nanos);
        await Assert.That(txId.Scheduled).IsTrue();
        await Assert.That(txId.ChildNonce).IsEqualTo(nonce);
    }

    [Test]
    public async Task Internal_Proto_AsTransactionId_Null_Returns_None()
    {
        TransactionID proto = null;
        var txId = proto.AsTransactionId();
        await Assert.That(txId).IsEqualTo(TransactionId.None);
    }
}
