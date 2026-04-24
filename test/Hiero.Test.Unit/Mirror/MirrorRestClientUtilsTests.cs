// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;

namespace Hiero.Test.Unit.Mirror;

public class MirrorRestClientUtilsTests
{
    [Test]
    public async Task MirrorFormat_Emits_Bare_Path_For_Default_TransactionId()
    {
        var txId = new TransactionId(new EntityId(0, 0, 1234), 1_700_000_000L, 123_456_789);
        var (path, filters) = MirrorRestClientUtils.MirrorFormat(txId);
        var result = MirrorRestClientUtils.GenerateInitialPath(path, filters);
        await Assert.That(result).IsEqualTo("0.0.1234-1700000000-123456789");
    }

    [Test]
    public async Task MirrorFormat_Appends_Nonce_When_ChildNonce_Is_NonZero()
    {
        var txId = new TransactionId(new EntityId(0, 0, 1234), 1_700_000_000L, 123_456_789, childNonce: 7);
        var (path, filters) = MirrorRestClientUtils.MirrorFormat(txId);
        var result = MirrorRestClientUtils.GenerateInitialPath(path, filters);
        await Assert.That(result).IsEqualTo("0.0.1234-1700000000-123456789?nonce=7");
    }

    [Test]
    public async Task MirrorFormat_Appends_Scheduled_When_Scheduled_Is_True()
    {
        var txId = new TransactionId(new EntityId(0, 0, 1234), 1_700_000_000L, 123_456_789, scheduled: true);
        var (path, filters) = MirrorRestClientUtils.MirrorFormat(txId);
        var result = MirrorRestClientUtils.GenerateInitialPath(path, filters);
        await Assert.That(result).IsEqualTo("0.0.1234-1700000000-123456789?scheduled=true");
    }

    [Test]
    public async Task MirrorFormat_Appends_Both_When_Nonce_And_Scheduled_Are_Set()
    {
        var txId = new TransactionId(new EntityId(0, 0, 1234), 1_700_000_000L, 123_456_789, scheduled: true, childNonce: 7);
        var (path, filters) = MirrorRestClientUtils.MirrorFormat(txId);
        var result = MirrorRestClientUtils.GenerateInitialPath(path, filters);
        await Assert.That(result).IsEqualTo("0.0.1234-1700000000-123456789?nonce=7&scheduled=true");
    }

    [Test]
    public async Task MirrorFormat_Returns_Empty_For_Null_TransactionId()
    {
        var (path, filters) = MirrorRestClientUtils.MirrorFormat((TransactionId)null!);
        var result = MirrorRestClientUtils.GenerateInitialPath(path, filters);
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task MirrorFormat_Pads_Nanos_To_Nine_Digits()
    {
        var txId = new TransactionId(new EntityId(0, 0, 1234), 1_700_000_000L, 42);
        var (path, filters) = MirrorRestClientUtils.MirrorFormat(txId);
        var result = MirrorRestClientUtils.GenerateInitialPath(path, filters);
        await Assert.That(result).IsEqualTo("0.0.1234-1700000000-000000042");
    }

    [Test]
    public async Task GenerateInitialPath_Returns_RootPath_When_No_Filters()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath("contracts/results/0xdeadbeef", []);
        await Assert.That(result).IsEqualTo("contracts/results/0xdeadbeef");
    }

    [Test]
    public async Task GenerateInitialPath_Uses_Question_Mark_For_Bare_RootPath()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results/0xdeadbeef",
            [HbarTransferProjectionFilter.Exclude]);
        await Assert.That(result).IsEqualTo("contracts/results/0xdeadbeef?hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Uses_Ampersand_When_RootPath_Already_Has_Query()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results/0.0.1234-1700000000-123456789?nonce=7",
            [HbarTransferProjectionFilter.Exclude]);
        await Assert.That(result).IsEqualTo("contracts/results/0.0.1234-1700000000-123456789?nonce=7&hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Joins_Multiple_Filters_With_Ampersand()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            [new PageLimit(50), HbarTransferProjectionFilter.Exclude]);
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false");
    }
}
