// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.Core;

public class KeyListTests
{
    [Test]
    public async Task Constructor_Maps_Endorsements_To_Keys()
    {
        var (publicKeyOne, _) = Generator.Ed25519KeyPair();
        var (publicKeyTwo, _) = Generator.Secp256k1KeyPair();
        var endorsements = new[]
        {
            new Endorsement(publicKeyOne),
            new Endorsement(publicKeyTwo)
        };

        var keyList = new KeyList(endorsements);

        await Assert.That(keyList.Keys.Count).IsEqualTo(2);
        await Assert.That(keyList.ToEndorsements()).IsEquivalentTo(endorsements);
    }
}
