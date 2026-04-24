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
}
