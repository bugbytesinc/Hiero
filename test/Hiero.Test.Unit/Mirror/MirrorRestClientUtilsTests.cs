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
    public async Task FormatMirror_Formats_Ecdsa_KeyAlias_As_The_Mirrors_Base32_Alias()
    {
        // Golden vector from testnet: account 0.0.10920714, auto-created from
        // this ECDSA key, which the mirror reports (and resolves) by the alias
        // below — the base32 of the protobuf-serialized Key, 3a21 + key bytes.
        var alias = new Endorsement(KeyType.ECDSASecp256K1, Convert.FromHexString("0269d9918b13afa8e105bef4962003d26a91c20f6a8c09f81a47d34b4c0cff5598"));
        var result = new EntityId(0, 0, alias).ToMirrorString();

        await Assert.That(result).IsEqualTo("HIQQE2OZSGFRHL5I4EC355EWEAB5E2URYIHWVDAJ7ANEPU2LJQGP6VMY");
    }

    [Test]
    public async Task FormatMirror_Formats_Ed25519_KeyAlias_As_Base32_Of_The_Protobuf_Key()
    {
        // 1220 + the 32 key bytes is the protobuf Key; expected value computed
        // independently (Python base64.b32encode, padding stripped).
        var alias = new Endorsement(KeyType.Ed25519, Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());
        var result = new EntityId(0, 0, alias).ToMirrorString();

        await Assert.That(result).IsEqualTo("CIQAAAICAMCAKBQHBAEQUCYMBUHA6EARCIJRIFIWC4MBSGQ3DQOR4HY");
    }

    [Test]
    public async Task FormatMirror_Prefixes_KeyAlias_With_Shard_And_Realm_Only_When_Not_Zero()
    {
        var alias = new Endorsement(KeyType.Ed25519, Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());
        const string encoded = "CIQAAAICAMCAKBQHBAEQUCYMBUHA6EARCIJRIFIWC4MBSGQ3DQOR4HY";

        await Assert.That(new EntityId(0, 0, alias).ToMirrorString()).IsEqualTo(encoded);
        await Assert.That(new EntityId(1, 2, alias).ToMirrorString()).IsEqualTo($"1.2.{encoded}");
        await Assert.That(new EntityId(0, 5, alias).ToMirrorString()).IsEqualTo($"0.5.{encoded}");
    }

    [Test]
    [Arguments("", "")]
    [Arguments("f", "MY")]
    [Arguments("fo", "MZXQ")]
    [Arguments("foo", "MZXW6")]
    [Arguments("foob", "MZXW6YQ")]
    [Arguments("fooba", "MZXW6YTB")]
    [Arguments("foobar", "MZXW6YTBOI")]
    public async Task Base32_Matches_Rfc4648_Test_Vectors_Without_Padding(string input, string expected)
    {
        // RFC 4648 section 10, padding removed as the mirror node expects.
        await Assert.That(Hiero.Implementation.Formatting.Base32Format.Encode(System.Text.Encoding.ASCII.GetBytes(input))).IsEqualTo(expected);
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
