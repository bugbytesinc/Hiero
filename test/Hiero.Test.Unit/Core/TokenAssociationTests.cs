// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TokenAssociationTests
{
    [Test]
    public async Task AsAssociationList_Returns_Empty_Array_For_Empty_List()
    {
        var list = new RepeatedField<TokenAssociation>();

        var result = list.AsAssociationList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task AsAssociationList_Maps_Associations()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var holderOne = new EntityId(0, 0, 1001);
        var holderTwo = new EntityId(0, 0, 1002);
        var list = new RepeatedField<TokenAssociation>();
        list.Add(new TokenAssociation { TokenId = new TokenID(tokenOne), AccountId = new AccountID(holderOne) });
        list.Add(new TokenAssociation { TokenId = new TokenID(tokenTwo), AccountId = new AccountID(holderTwo) });

        var result = list.AsAssociationList();

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsEqualTo(new Association(list[0]));
        await Assert.That(result[1]).IsEqualTo(new Association(list[1]));
    }
}
