// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero.Test.Unit.Crypto;

public class AccountBalancesTests
{
    [Test]
    public async Task Constructor_Returns_Empty_Token_Dictionary_For_No_Tokens()
    {
        var holder = new EntityId(0, 0, 1001);
        var response = new Response
        {
            CryptogetAccountBalance = new CryptoGetAccountBalanceResponse
            {
                AccountID = new AccountID(holder),
                Balance = 100
            }
        };

        var result = new AccountBalances(response);

        await Assert.That(result.Holder).IsEqualTo(holder);
        await Assert.That(result.Crypto).IsEqualTo(100UL);
#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(result.Tokens).IsEmpty();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Test]
    public async Task Constructor_Aggregates_Duplicate_Token_Balances()
    {
        var holder = new EntityId(0, 0, 1001);
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var response = new Response
        {
            CryptogetAccountBalance = new CryptoGetAccountBalanceResponse
            {
                AccountID = new AccountID(holder),
                Balance = 100
            }
        };
#pragma warning disable CS0612 // Type or member is obsolete
        response.CryptogetAccountBalance.TokenBalances.Add(new Proto.TokenBalance { TokenId = new TokenID(tokenOne), Balance = 10, Decimals = 8 });
        response.CryptogetAccountBalance.TokenBalances.Add(new Proto.TokenBalance { TokenId = new TokenID(tokenOne), Balance = 15, Decimals = 8 });
        response.CryptogetAccountBalance.TokenBalances.Add(new Proto.TokenBalance { TokenId = new TokenID(tokenTwo), Balance = 20, Decimals = 2 });
#pragma warning restore CS0612 // Type or member is obsolete

        var result = new AccountBalances(response);

#pragma warning disable CS0618 // Type or member is obsolete
        await Assert.That(result.Tokens.Count).IsEqualTo(2);
        await Assert.That(result.Tokens[tokenOne].Balance).IsEqualTo(25UL);
        await Assert.That(result.Tokens[tokenOne].Decimals).IsEqualTo(8U);
        await Assert.That(result.Tokens[tokenTwo].Balance).IsEqualTo(20UL);
        await Assert.That(result.Tokens[tokenTwo].Decimals).IsEqualTo(2U);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
