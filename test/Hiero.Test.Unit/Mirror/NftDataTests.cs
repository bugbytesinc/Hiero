// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class NftDataTests
{
    [Test]
    public async Task Metadata_Field_Deserializes_Base64_To_Bytes()
    {
        var json = """{"token_id":"0.0.100","serial_number":1,"metadata":"VGhpcyBpcyBhIHRlc3QgTkZU"}""";
        var data = JsonSerializer.Deserialize<NftData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(System.Text.Encoding.UTF8.GetString(data!.Metadata.Span)).IsEqualTo("This is a test NFT");
    }

    [Test]
    public async Task Metadata_Field_Default_Is_Empty()
    {
        var json = """{"token_id":"0.0.100","serial_number":1}""";
        var data = JsonSerializer.Deserialize<NftData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Metadata.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Metadata_Field_Roundtrips_Raw_Bytes()
    {
        var expected = new byte[] { 0x00, 0x01, 0xff, 0xfe, 0xa5 };
        var base64 = Convert.ToBase64String(expected);
        var json = $$"""{"token_id":"0.0.100","serial_number":1,"metadata":"{{base64}}"}""";
        var data = JsonSerializer.Deserialize<NftData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Metadata.Span.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Page_Envelope_Reads_Nfts_Array()
    {
        var json = """
        {
            "nfts":[
                {"token_id":"0.0.100","serial_number":1,"account_id":"0.0.500"},
                {"token_id":"0.0.100","serial_number":2,"account_id":"0.0.500"},
                {"token_id":"0.0.101","serial_number":7,"account_id":"0.0.500"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.NftDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(3);
        await Assert.That(items[0].Token).IsEqualTo(new EntityId(0, 0, 100));
        await Assert.That(items[0].SerialNumber).IsEqualTo(1L);
        await Assert.That(items[2].Token).IsEqualTo(new EntityId(0, 0, 101));
        await Assert.That(items[2].SerialNumber).IsEqualTo(7L);
    }
}
