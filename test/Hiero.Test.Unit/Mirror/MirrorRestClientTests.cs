// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

namespace Hiero.Test.Unit.Mirror;

public class MirrorRestClientTests
{
    [Test]
    public async Task Constructor_Null_Client_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => { new MirrorRestClient(null); });
        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.ParamName).IsEqualTo("client");
    }

    [Test]
    public async Task EndpointUrl_Returns_BaseAddress()
    {
        var baseUrl = "https://testnet.mirrornode.hedera.com/";
        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var mirrorClient = new MirrorRestClient(httpClient);
        await Assert.That(mirrorClient.EndpointUrl).IsEqualTo(baseUrl);
    }

    [Test]
    public async Task EndpointUrl_Returns_Empty_String_When_No_BaseAddress()
    {
        var httpClient = new HttpClient();
        var mirrorClient = new MirrorRestClient(httpClient);
        await Assert.That(mirrorClient.EndpointUrl).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ToString_Returns_Useful_String()
    {
        var baseUrl = "https://testnet.mirrornode.hedera.com/";
        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var mirrorClient = new MirrorRestClient(httpClient);
        var result = mirrorClient.ToString();
        await Assert.That(result).Contains(baseUrl.TrimEnd('/'));
    }
}
