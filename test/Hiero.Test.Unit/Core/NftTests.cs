// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class NftTests
{
    [Test]
    public async Task ToString_Returns_TokenHashSerial_Format()
    {
        var nft = new Hiero.Nft(new EntityId(0, 0, 5), 3);

        await Assert.That(nft.ToString()).IsEqualTo("0.0.5#3");
    }
}
