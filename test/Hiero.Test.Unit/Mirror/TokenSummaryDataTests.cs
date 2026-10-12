// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class TokenSummaryDataTests
{
    [Test]
    public async Task Deserializes_Fungible_Record_From_OpenAPI_Example()
    {
        var json = """
        {
            "admin_key":null,
            "decimals":3,
            "metadata":"VGhpcyBpcyBhIHRlc3QgTkZU",
            "name":"First Mover",
            "symbol":"FIRSTMOVERLPDJH",
            "token_id":"0.0.1",
            "type":"FUNGIBLE_COMMON"
        }
        """;
        var data = JsonSerializer.Deserialize<TokenSummaryData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Administrator).IsNull();
        await Assert.That(data.Decimals).IsEqualTo(3);
        await Assert.That(System.Text.Encoding.UTF8.GetString(data.Metadata.Span)).IsEqualTo("This is a test NFT");
        await Assert.That(data.Name).IsEqualTo("First Mover");
        await Assert.That(data.Symbol).IsEqualTo("FIRSTMOVERLPDJH");
        await Assert.That(data.Token).IsEqualTo(new EntityId(0, 0, 1));
        await Assert.That(data.Type).IsEqualTo(TokenType.Fungible);
    }

    [Test]
    public async Task Deserializes_NonFungible_Record()
    {
        var json = """
        {
            "decimals":0,
            "name":"Art Collection",
            "symbol":"ART",
            "token_id":"0.0.200",
            "type":"NON_FUNGIBLE_UNIQUE"
        }
        """;
        var data = JsonSerializer.Deserialize<TokenSummaryData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Decimals).IsEqualTo(0);
        await Assert.That(data.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(data.Metadata.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Defaults_Are_Safe_When_Fields_Absent()
    {
        var json = """{}""";
        var data = JsonSerializer.Deserialize<TokenSummaryData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Administrator).IsNull();
        await Assert.That(data.Decimals).IsEqualTo(0);
        await Assert.That(data.Metadata.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Page_Envelope_Reads_Tokens_Array()
    {
        var json = """
        {
            "tokens":[
                {"decimals":3,"name":"First Mover","symbol":"FIRSTMOVERLPDJH","token_id":"0.0.1","type":"FUNGIBLE_COMMON"},
                {"decimals":0,"name":"Art Collection","symbol":"ART","token_id":"0.0.200","type":"NON_FUNGIBLE_UNIQUE"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.TokenSummaryDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(items[1].Type).IsEqualTo(TokenType.NonFungible);
    }
}
