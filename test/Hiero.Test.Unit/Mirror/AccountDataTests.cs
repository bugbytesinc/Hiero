// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class AccountDataTests
{
    [Test]
    public async Task Delegated_Account_Reads_Its_Delegation_Address()
    {
        var json = """{"account":"0.0.1001","delegation_address":"0x0000000000000000000000000000000000000001"}""";
        var account = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.AccountData)!;
        await Assert.That(account.DelegationAddress!.ToString()).IsEqualTo("0x0000000000000000000000000000000000000001");
    }

    [Test]
    public async Task Account_Without_Delegation_Reads_Null()
    {
        var explicitNull = JsonSerializer.Deserialize("""{"account":"0.0.1001","delegation_address":null}""", MirrorJsonContext.Default.AccountData)!;
        await Assert.That(explicitNull.DelegationAddress).IsNull();

        // Mainnet and testnet omit the field for a plain account.
        var absent = JsonSerializer.Deserialize("""{"account":"0.0.1001"}""", MirrorJsonContext.Default.AccountData)!;
        await Assert.That(absent.DelegationAddress).IsNull();
    }

    [Test]
    public async Task Token_Holdings_And_Balances_Read_Decimals()
    {
        // As served by mainnet for /accounts/{id}/tokens and
        // /tokens/{id}/balances.
        var holdings = """
        {
            "tokens": [
                { "automatic_association": true, "balance": 9317215, "created_timestamp": "1791464513.396954104", "decimals": 6, "token_id": "0.0.456858", "freeze_status": "UNFROZEN", "kyc_status": "NOT_APPLICABLE" }
            ],
            "links": { "next": null }
        }
        """;
        var holding = JsonSerializer.Deserialize(holdings, MirrorJsonContext.Default.TokenHoldingDataPage)!.GetItems()[0];
        await Assert.That(holding.Decimals).IsEqualTo(6);
        await Assert.That(holding.Balance).IsEqualTo(9317215L);

        var balances = """{"timestamp":"1791464513.396954104","balances":[{"account":"0.0.10910254","balance":9317215,"decimals":6}],"links":{"next":null}}""";
        var balance = JsonSerializer.Deserialize(balances, MirrorJsonContext.Default.AccountBalanceDataPage)!.GetItems()[0];
        await Assert.That(balance.Decimals).IsEqualTo(6);
        await Assert.That(balance.Account).IsEqualTo(new EntityId(0, 0, 10910254));
    }
}
