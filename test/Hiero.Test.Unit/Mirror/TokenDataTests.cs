// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class TokenDataTests
{
    [Test]
    public async Task Metadata_Field_Deserializes_Base64_To_Bytes()
    {
        var json = """{"token_id":"0.0.100","metadata":"VGhpcyBpcyBhIHRlc3QgTkZU"}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(System.Text.Encoding.UTF8.GetString(data!.Metadata.Span)).IsEqualTo("This is a test NFT");
    }

    [Test]
    public async Task Metadata_Field_Default_Is_Empty()
    {
        var json = """{"token_id":"0.0.100"}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Metadata.Length).IsEqualTo(0);
    }

    [Test]
    public async Task MetadataEndorsement_Deserializes_From_Key_Object()
    {
        var json = """
        {
            "token_id":"0.0.100",
            "metadata_key":{"_type":"ED25519","key":"302a300506032b65700321001234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"}
        }
        """;
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.MetadataEndorsement).IsNotNull();
    }

    [Test]
    public async Task MetadataEndorsement_Default_Is_Null()
    {
        var json = """{"token_id":"0.0.100"}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.MetadataEndorsement).IsNull();
    }

    [Test]
    public async Task MetadataEndorsement_Absent_Key_Is_Null()
    {
        var json = """{"token_id":"0.0.100","metadata_key":null}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.MetadataEndorsement).IsNull();
    }

    [Test]
    public async Task A_Null_Expiry_Timestamp_Reads_As_MinValue()
    {
        var json = """{"token_id":"0.0.100","expiry_timestamp":null}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data!.Expiration).IsEqualTo(ConsensusTimeStamp.MinValue);
    }

    [Test]
    public async Task An_Expiry_Timestamp_In_Nanoseconds_Is_Read()
    {
        var json = """{"token_id":"0.0.100","expiry_timestamp":1700000000123456789}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data!.Expiration).IsEqualTo(new ConsensusTimeStamp(1_700_000_000, 123_456_789));
    }

    [Test]
    public async Task Delegatable_Contract_Keys_From_A_Mirror_Node_Are_Read()
    {
        // The admin key of a mainnet token whose keys are managed by its
        // proxy contract, exactly as the mirror node reports it.
        var json = """{"token_id":"0.0.8317071","admin_key":{"_type":"ProtobufEncoded","key":"4205188ed1fb03"}}""";
        var data = JsonSerializer.Deserialize<TokenData>(json);
        await Assert.That(data!.Administrator!.Type).IsEqualTo(KeyType.DelegatableContract);
        await Assert.That(data.Administrator.Contract).IsEqualTo(new EntityId(0, 0, 8317070));
        await Assert.That(JsonSerializer.Serialize(data.Administrator, new JsonSerializerOptions { Converters = { new Hiero.Converters.EndorsementConverter() } }))
            .IsEqualTo("""{"_type":"ProtobufEncoded","key":"4205188ed1fb03"}""");
    }
}
