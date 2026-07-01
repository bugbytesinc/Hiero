// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TokenRelationshipTests
{
    [Test]
    public async Task ToBalances_Returns_Empty_Array_For_Empty_List()
    {
        var list = new RepeatedField<TokenRelationship>();

        var result = list.ToBalances();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task ToBalances_Maps_Token_Relationships()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var list = new RepeatedField<TokenRelationship>();
        list.Add(new TokenRelationship
        {
            TokenId = new TokenID(tokenOne),
            Symbol = "ONE",
            Balance = 10,
            Decimals = 2,
            KycStatus = Proto.TokenKycStatus.Granted,
            FreezeStatus = Proto.TokenFreezeStatus.Unfrozen,
            AutomaticAssociation = true
        });
        list.Add(new TokenRelationship
        {
            TokenId = new TokenID(tokenTwo),
            Symbol = "TWO",
            Balance = 20,
            Decimals = 3,
            KycStatus = Proto.TokenKycStatus.Revoked,
            FreezeStatus = Proto.TokenFreezeStatus.Frozen
        });

        var result = list.ToBalances();

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Token).IsEqualTo(tokenOne);
        await Assert.That(result[0].Symbol).IsEqualTo("ONE");
        await Assert.That(result[0].Balance).IsEqualTo(10UL);
        await Assert.That(result[0].Decimals).IsEqualTo(2U);
        await Assert.That(result[0].AutoAssociated).IsTrue();
        await Assert.That(result[1].Token).IsEqualTo(tokenTwo);
        await Assert.That(result[1].Symbol).IsEqualTo("TWO");
        await Assert.That(result[1].Balance).IsEqualTo(20UL);
        await Assert.That(result[1].Decimals).IsEqualTo(3U);
        await Assert.That(result[1].AutoAssociated).IsFalse();
    }
}
