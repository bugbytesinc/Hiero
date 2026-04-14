// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class ConsensusTimeStampTests
{
    [Test]
    public async Task Equivalent_TimeStamps_Are_Considered_Equal()
    {
        var now = DateTime.UtcNow;
        var ts1 = new ConsensusTimeStamp(now);
        var ts2 = new ConsensusTimeStamp(now);
        await Assert.That(ts1).IsEqualTo(ts2);
        await Assert.That(ts1 == ts2).IsTrue();
        await Assert.That(ts1 != ts2).IsFalse();
        await Assert.That(ts1 >= ts2).IsTrue();
        await Assert.That(ts1 <= ts2).IsTrue();
        await Assert.That(ts1.Equals(ts2)).IsTrue();
        await Assert.That(ts2.Equals(ts1)).IsTrue();
    }

    [Test]
    public async Task Disimilar_TimeStamps_Are_Not_Considered_Equal()
    {
        var ts1 = new ConsensusTimeStamp(DateTime.UtcNow);
        var ts2 = new ConsensusTimeStamp(DateTime.UtcNow.AddSeconds(1));
        await Assert.That(ts1).IsNotEqualTo(ts2);
        await Assert.That(ts1 == ts2).IsFalse();
        await Assert.That(ts1 != ts2).IsTrue();
        await Assert.That(ts1 >= ts2).IsFalse();
        await Assert.That(ts1 <= ts2).IsTrue();
        await Assert.That(ts1.Equals(ts2)).IsFalse();
        await Assert.That(ts2.Equals(ts1)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var ts = new ConsensusTimeStamp(DateTime.UtcNow);
        await Assert.That(ts.Equals("Something that is not a timestamp")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var ts = new ConsensusTimeStamp(DateTime.UtcNow);
        object equivalent = new ConsensusTimeStamp(ts.Seconds);
        await Assert.That(ts.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(ts)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var ts = new ConsensusTimeStamp(DateTime.UtcNow);
        object reference = ts;
        await Assert.That(ts.Equals(ts)).IsTrue();
        await Assert.That(reference.Equals(ts)).IsTrue();
    }

    [Test]
    public async Task Can_Compute_Difference_In_Seconds()
    {
        var diff = 100m;
        var ts1 = new ConsensusTimeStamp(DateTime.UtcNow);
        var ts2 = new ConsensusTimeStamp(ts1.Seconds + diff);
        await Assert.That(ts2 > ts1).IsTrue();
        await Assert.That(ts2 - ts1).IsEqualTo(diff);
    }

    [Test]
    public async Task Can_Construct_With_Nanoseconds()
    {
        var seconds = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        var nanos = 99;
        var value = decimal.Add(seconds, decimal.Divide(nanos, 1000000000m));
        var ts = new ConsensusTimeStamp(seconds, nanos);
        await Assert.That(ts.Seconds).IsEqualTo(value);
    }

    [Test]
    public async Task ToString_Includes_Fraction()
    {
        var seconds = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        var nanos = 99;
        await Assert.That(new ConsensusTimeStamp(seconds, nanos).ToString()).IsEqualTo($"{seconds}.000000099");
        var value = decimal.Add(seconds, decimal.Divide(nanos, 1000000000m));
        await Assert.That(new ConsensusTimeStamp(value).ToString()).IsEqualTo($"{seconds}.000000099");
        await Assert.That(new ConsensusTimeStamp(10, 0).ToString()).IsEqualTo("10.000000000");
        await Assert.That(new ConsensusTimeStamp(1000.1m).ToString()).IsEqualTo("1000.100000000");
        await Assert.That(new ConsensusTimeStamp(-1000.1m).ToString()).IsEqualTo("-1000.100000000");
        await Assert.That(new ConsensusTimeStamp(0, 100000000).ToString()).IsEqualTo("0.100000000");
    }

    [Test]
    public async Task Can_Implicitly_Cast_DateTime_To_ConsensusTimeStamp()
    {
        var now = DateTime.UtcNow;
        var ts1 = new ConsensusTimeStamp(now);
        ConsensusTimeStamp ts2 = now;
        await Assert.That(ts1).IsEqualTo(ts2);
        await Assert.That(ts1).IsEqualTo(now);
        await Assert.That(ts2).IsEqualTo(now);
        await Assert.That(ts1 == now).IsTrue();
        await Assert.That(ts2 == now).IsTrue();
        await Assert.That(ts2 != now).IsFalse();
        await Assert.That(ts2 < now).IsFalse();
        await Assert.That(ts2 > now).IsFalse();
        await Assert.That(ts2 <= now).IsTrue();
        await Assert.That(ts2 >= now).IsTrue();
    }

    [Test]
    public async Task CompareTo_Returns_Correct_Ordering()
    {
        var ts1 = new ConsensusTimeStamp(100, 0);
        var ts2 = new ConsensusTimeStamp(200, 0);
        var ts3 = new ConsensusTimeStamp(100, 0);
        await Assert.That(ts1.CompareTo(ts2)).IsNegative();
        await Assert.That(ts2.CompareTo(ts1)).IsPositive();
        await Assert.That(ts1.CompareTo(ts3)).IsEqualTo(0);
    }

    [Test]
    public async Task CompareTo_Object_Returns_Correct_Ordering()
    {
        var ts1 = new ConsensusTimeStamp(100, 0);
        object ts2 = new ConsensusTimeStamp(200, 0);
        object notATimestamp = "not a timestamp";
        await Assert.That(ts1.CompareTo(ts2)).IsNegative();
        await Assert.That(ts1.CompareTo(notATimestamp)).IsPositive();
    }

    [Test]
    public async Task MinValue_And_MaxValue_Are_Correct()
    {
        await Assert.That(ConsensusTimeStamp.MinValue.Seconds).IsEqualTo(decimal.MinValue);
        await Assert.That(ConsensusTimeStamp.MaxValue.Seconds).IsEqualTo(decimal.MaxValue);
        await Assert.That(ConsensusTimeStamp.MinValue < ConsensusTimeStamp.MaxValue).IsTrue();
    }

    [Test]
    public async Task Default_Constructor_Is_MinValue()
    {
        var ts = new ConsensusTimeStamp();
        await Assert.That(ts).IsEqualTo(ConsensusTimeStamp.MinValue);
    }

    [Test]
    public async Task Now_Returns_Approximate_Current_Time()
    {
        var before = new ConsensusTimeStamp(DateTime.UtcNow);
        var now = ConsensusTimeStamp.Now;
        var after = new ConsensusTimeStamp(DateTime.UtcNow);
        await Assert.That(now >= before).IsTrue();
        await Assert.That(now <= after).IsTrue();
    }

    [Test]
    public async Task Equal_TimeStamps_Have_Equal_HashCodes()
    {
        var now = DateTime.UtcNow;
        var ts1 = new ConsensusTimeStamp(now);
        var ts2 = new ConsensusTimeStamp(now);
        await Assert.That(ts1.GetHashCode()).IsEqualTo(ts2.GetHashCode());
    }

    [Test]
    public async Task Less_Than_And_Greater_Than_Operators()
    {
        var ts1 = new ConsensusTimeStamp(100, 0);
        var ts2 = new ConsensusTimeStamp(200, 0);
        await Assert.That(ts1 < ts2).IsTrue();
        await Assert.That(ts2 < ts1).IsFalse();
        await Assert.That(ts2 > ts1).IsTrue();
        await Assert.That(ts1 > ts2).IsFalse();
    }
}
