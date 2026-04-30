using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Mirror;

public class MirrorAccountDataTests
{
    [Test]
    public async Task Can_Get_Accounts_Filtered_By_Id()
    {
        // Use AccountFilter.Is(...) to narrow the network-wide list down
        // to a single fixture account so we don't paginate through testnet.
        await using var fxAccount = await TestAccount.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var matches = new List<AccountData>();
        await foreach (var data in mirror.GetAccountsAsync(AccountFilter.Is(fxAccount)))
        {
            matches.Add(data);
            if (matches.Count >= 5)
            {
                break;
            }
        }

        await Assert.That(matches.Count).IsEqualTo(1);
        await Assert.That(matches[0].Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(matches[0].Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(matches[0].Deleted).IsFalse();
    }

    [Test]
    public async Task Can_Get_Accounts_Filtered_By_Public_Key()
    {
        // Mirror lookup by raw public key — same code path as TestNetwork's
        // payer discovery uses, but exercised against a fresh fixture so we
        // assert exactly one match.
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Ed25519KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var matches = new List<AccountData>();
        await foreach (var data in mirror.GetAccountsAsync(AccountPublicKeyFilter.Is(fxAccount.PublicKey)))
        {
            matches.Add(data);
            if (matches.Count >= 5)
            {
                break;
            }
        }
        // A fresh random key should match exactly the one fixture account.
        await Assert.That(matches.Count).IsEqualTo(1);
        await Assert.That(matches[0].Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Get_Account_Staking_Rewards()
    {
        // A freshly-created fixture account has never been staked, so the
        // mirror returns an empty list. We assert the enumerator completes
        // normally (no exception, zero items) — that's the load-bearing
        // behavior of the method.
        await using var fxAccount = await TestAccount.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var rewards = new List<StakingRewardData>();
        await foreach (var reward in mirror.GetAccountStakingRewardsAsync(fxAccount, new PageLimit(10)))
        {
            rewards.Add(reward);
            if (rewards.Count >= 10)
            {
                break;
            }
        }
        await Assert.That(rewards).IsEmpty();
    }

    [Test]
    public async Task Can_Get_Nft_Allowances_From_Both_Perspectives()
    {
        // Create an NFT allowance — owner grants spender permission to
        // transfer all serials of a token class — then read it back from
        // both the owner-perspective and spender-perspective endpoints.
        await using var fxNft = await TestNft.CreateAsync();
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var owner = fxNft.TreasuryAccount.CreateReceipt!.Address;
        var spender = fxAgent.CreateReceipt!.Address;

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = new[] { new NftAllowance(fxNft.CreateReceipt!.Token, fxNft.TreasuryAccount, fxAgent) },
            Signatory = fxNft.TreasuryAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        // Owner-perspective: owner sees outgoing allowance, target spender = fxAgent.
        var asOwner = new List<NftAllowanceData>();
        await foreach (var data in mirror.GetAccountNftAllowancesAsOwnerAsync(owner))
        {
            asOwner.Add(data);
        }
        await Assert.That(asOwner.Count).IsEqualTo(1);
        await Assert.That(asOwner[0].Owner).IsEqualTo(owner);
        await Assert.That(asOwner[0].Spender).IsEqualTo(spender);
        await Assert.That(asOwner[0].Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(asOwner[0].ApprovedForAll).IsTrue();
        await Assert.That(asOwner[0].Timestamp).IsNotNull();

        // Spender-perspective: spender sees incoming allowance, source owner = treasury.
        var asSpender = new List<NftAllowanceData>();
        await foreach (var data in mirror.GetAccountNftAllowancesAsSpenderAsync(spender))
        {
            asSpender.Add(data);
        }
        await Assert.That(asSpender.Count).IsEqualTo(1);
        await Assert.That(asSpender[0].Owner).IsEqualTo(owner);
        await Assert.That(asSpender[0].Spender).IsEqualTo(spender);
        await Assert.That(asSpender[0].Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(asSpender[0].ApprovedForAll).IsTrue();
    }
}
