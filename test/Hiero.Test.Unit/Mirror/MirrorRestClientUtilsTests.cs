// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Mirror;

public class MirrorRestClientUtilsTests
{
    [Test]
    public async Task FormatMirror_Formats_ShardRealmNum_EntityId()
    {
        var result = new EntityId(0, 0, 1234).ToMirrorString();

        await Assert.That(result).IsEqualTo("0.0.1234");
    }

    [Test]
    public async Task FormatMirror_Formats_EvmAddress_EntityId_As_UnprefixedHex()
    {
        var bytes = new byte[20];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i + 1);
        }
        var result = new EntityId(0, 0, new EvmAddress(bytes)).ToMirrorString();

        await Assert.That(result).IsEqualTo(Convert.ToHexStringLower(bytes));
    }

    [Test]
    public async Task FormatMirror_Formats_KeyAlias_EntityId_As_MirrorHex()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var alias = new Endorsement(publicKey);
        var result = new EntityId(0, 0, alias).ToMirrorString();

        await Assert.That(result).IsEqualTo(Convert.ToHexStringLower(alias.ToBytes(KeyFormat.Mirror).Span));
    }

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
            HbarTransferProjectionFilter.Exclude);
        await Assert.That(result).IsEqualTo("contracts/results/0xdeadbeef?hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Uses_Ampersand_When_RootPath_Already_Has_Query()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results/0.0.1234-1700000000-123456789?nonce=7",
            HbarTransferProjectionFilter.Exclude);
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

    [Test]
    public async Task GenerateInitialPath_Joins_One_Array_Filter()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            [HbarTransferProjectionFilter.Exclude]);
        await Assert.That(result).IsEqualTo("contracts/results?hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Joins_Three_Array_Filters()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            [new PageLimit(50), HbarTransferProjectionFilter.Exclude, new TestFilter("order", "asc")]);
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false&order=asc");
    }

    [Test]
    public async Task GenerateInitialPath_Joins_Two_Fixed_Filters_With_Ampersand()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            new PageLimit(50),
            HbarTransferProjectionFilter.Exclude);
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Joins_Three_Fixed_Filters_With_Ampersand()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            new PageLimit(50),
            HbarTransferProjectionFilter.Exclude,
            new TestFilter("order", "asc"));
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false&order=asc");
    }

    [Test]
    public async Task GenerateInitialPath_Uses_Fixed_Filter_Path_When_Optional_Filters_Are_Empty()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            HbarTransferProjectionFilter.Exclude,
            []);
        await Assert.That(result).IsEqualTo("contracts/results?hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Uses_Two_Fixed_Filter_Path_When_Optional_Filters_Are_Empty()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            new PageLimit(50),
            HbarTransferProjectionFilter.Exclude,
            []);
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Joins_Two_Filter_Arrays()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            [new PageLimit(50)],
            [HbarTransferProjectionFilter.Exclude]);
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false");
    }

    [Test]
    public async Task GenerateInitialPath_Joins_Required_Filter_And_Two_Filter_Arrays()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            new PageLimit(50),
            [HbarTransferProjectionFilter.Exclude],
            [new TestFilter("order", "asc")]);
        await Assert.That(result).IsEqualTo("contracts/results?limit=50&hbar=false&order=asc");
    }

    [Test]
    public async Task GenerateInitialPath_Url_Encodes_Filter_Names_And_Values()
    {
        var result = MirrorRestClientUtils.GenerateInitialPath(
            "contracts/results",
            new TestFilter("memo value", "alpha beta&gamma"));
        await Assert.That(result).IsEqualTo("contracts/results?memo+value=alpha+beta%26gamma");
    }

    private sealed class TestFilter(string name, string value) : IMirrorFilter
    {
        public string Name { get; } = name;
        public string Value { get; } = value;
    }
}
