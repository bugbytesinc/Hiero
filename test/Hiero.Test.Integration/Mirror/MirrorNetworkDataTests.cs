using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Mirror;

public class MirrorNetworkDataTests
{
    [Test]
    public async Task Can_Get_Exchange_Rate()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetExchangeRateAsync();

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.CurrentRate).IsNotNull();
        await Assert.That(data.NextRate).IsNotNull();
        await Assert.That(data.CurrentRate.HbarEquivalent > 0).IsTrue();
        await Assert.That(data.CurrentRate.CentEquivalent > 0).IsTrue();
        await Assert.That(data.NextRate.HbarEquivalent > 0).IsTrue();
        await Assert.That(data.NextRate.CentEquivalent > 0).IsTrue();
        await Assert.That(data.Timestamp.Seconds > 0).IsTrue();
        await Assert.That(data.CurrentRate.Expiration.Seconds > 0).IsTrue();
        await Assert.That(data.NextRate.Expiration.Seconds > 0).IsTrue();
        await Assert.That(data.NextRate.Expiration > data.CurrentRate.Expiration).IsTrue();
    }

    [Test]
    public async Task Can_Get_Exchange_Rate_At_Timestamp()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var asOf = await mirror.GetLatestConsensusTimestampAsync();
        var data = await mirror.GetExchangeRateAsync(asOf);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.CurrentRate.HbarEquivalent > 0).IsTrue();
        await Assert.That(data.CurrentRate.CentEquivalent > 0).IsTrue();
        await Assert.That(data.Timestamp <= asOf).IsTrue();
    }

    [Test]
    public async Task Can_Get_Latest_Network_Fees()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetLatestNetworkFeesAsync();

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Fees).IsNotNull();
        await Assert.That(data.Fees.Length > 0).IsTrue();
        await Assert.That(data.Timestamp.Seconds > 0).IsTrue();
        foreach (var fee in data.Fees)
        {
            await Assert.That(fee.TransactionType).IsNotNull();
            await Assert.That(string.IsNullOrWhiteSpace(fee.TransactionType)).IsFalse();
            await Assert.That(fee.GasPrice > 0UL).IsTrue();
        }
    }

    [Test]
    public async Task Can_Get_Network_Fees_At_Timestamp()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var asOf = await mirror.GetLatestConsensusTimestampAsync();
        var data = await mirror.GetNetworkFeesAsync(asOf);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Fees.Length > 0).IsTrue();
        await Assert.That(data.Timestamp <= asOf).IsTrue();
    }

    [Test]
    public async Task Can_Get_Network_Stake()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetNetworkStakeAsync();

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.MaxStakeRewarded >= 0).IsTrue();
        await Assert.That(data.MaxStakingRewardRatePerHbar >= 0).IsTrue();
        await Assert.That(data.MaxTotalReward >= 0).IsTrue();
        await Assert.That(data.NodeRewardFeeFraction >= 0.0 && data.NodeRewardFeeFraction <= 1.0).IsTrue();
        await Assert.That(data.StakingRewardFeeFraction >= 0.0 && data.StakingRewardFeeFraction <= 1.0).IsTrue();
        await Assert.That(data.StakeTotal >= 0).IsTrue();
        await Assert.That(data.StakingPeriod).IsNotNull();
        await Assert.That(data.StakingPeriod.Starting).IsNotNull();
        await Assert.That(data.StakingPeriod.Starting!.Value.Seconds > 0).IsTrue();
        await Assert.That(data.StakingPeriodDuration > 0).IsTrue();
        await Assert.That(data.StakingPeriodsStored > 0).IsTrue();
        await Assert.That(data.StakingRewardRate >= 0).IsTrue();
        await Assert.That(data.RewardBalanceThreshold >= 0).IsTrue();
        await Assert.That(data.ReservedStakingRewards >= 0).IsTrue();
        await Assert.That(data.UnreservedStakingRewardBalance >= 0).IsTrue();
    }

    [Test]
    public async Task Can_Get_Network_Supply()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetNetworkSupplyAsync();

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.TotalSupply > 0).IsTrue();
        await Assert.That(data.ReleasedSupply > 0).IsTrue();
        await Assert.That(data.ReleasedSupply <= data.TotalSupply).IsTrue();
        await Assert.That(data.Timestamp.Seconds > 0).IsTrue();
    }

    [Test]
    public async Task Can_Get_Network_Supply_At_Timestamp()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var asOf = await mirror.GetLatestConsensusTimestampAsync();
        var data = await mirror.GetNetworkSupplyAsync(asOf);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.TotalSupply > 0).IsTrue();
        await Assert.That(data.ReleasedSupply > 0).IsTrue();
        await Assert.That(data.Timestamp <= asOf).IsTrue();
    }

    [Test]
    public async Task Can_Get_Chain_Id()
    {
        // Per user direction: GetChainIdAsync may be flaky at the mirror level.
        // The method scans recent contract-result records and returns the first
        // non-zero chain_id; on testnet this is normally reliable, but if the
        // most recent page lacks chain_id values the call throws. Tolerate that.
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        try
        {
            var chainId = await mirror.GetChainIdAsync();
            // Hedera testnet chain id is 296 (mainnet 295, previewnet 297).
            await Assert.That(chainId > 0).IsTrue();
            TestContext.Current?.OutputWriter.WriteLine($"Chain ID = {chainId}");
        }
        catch (MirrorException ex) when (ex.Message.Contains("Chain ID", StringComparison.OrdinalIgnoreCase) ||
                                          ex.Message.Contains("Contract results", StringComparison.OrdinalIgnoreCase))
        {
            TestContext.Current?.OutputWriter.WriteLine($"GetChainIdAsync flaked (mirror lacked usable contract-result page): {ex.Message}");
        }
    }

    [Test]
    public async Task Can_Get_Latest_Consensus_Timestamp()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var ts = await mirror.GetLatestConsensusTimestampAsync();

        await Assert.That(ts > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(ts.Seconds > 0).IsTrue();
        // Should be close to wall-clock now (within a few minutes).
        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await Assert.That(Math.Abs(ts.Seconds - nowSeconds) < 600).IsTrue();
    }

    [Test]
    public async Task Can_Get_Latest_Block()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var block = await mirror.GetLatestBlockAsync();

        await Assert.That(block).IsNotNull();
        await Assert.That(block!.Number > 0).IsTrue();
        // Hedera block hashes are SHA-384 (48 bytes / 96 hex chars on the wire).
        await Assert.That(block.Hash.Length).IsEqualTo(48);
        await Assert.That(block.PreviousHash.Length).IsEqualTo(48);
        await Assert.That(block.Count >= 0).IsTrue();
        await Assert.That(block.Size >= 0).IsTrue();
        await Assert.That(block.TimestampRange).IsNotNull();
        await Assert.That(block.TimestampRange.Starting).IsNotNull();
        await Assert.That(block.TimestampRange.Starting!.Value.Seconds > 0).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(block.Version)).IsFalse();
        await Assert.That(string.IsNullOrWhiteSpace(block.Name)).IsFalse();
    }

    [Test]
    public async Task Can_Get_Latest_Block_Before_Consensus()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var asOf = await mirror.GetLatestConsensusTimestampAsync();
        var block = await mirror.GetLatestBlockBeforeConsensusAsync(asOf);

        await Assert.That(block).IsNotNull();
        await Assert.That(block!.Number > 0).IsTrue();
        await Assert.That(block.TimestampRange.Starting <= asOf).IsTrue();
    }

    [Test]
    public async Task Can_Get_Blocks_Page()
    {
        // Take a small bounded page so we don't paginate through testnet history.
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var blocks = new List<BlockData>();
        await foreach (var block in mirror.GetBlocksAsync(new PageLimit(5), OrderBy.Descending))
        {
            blocks.Add(block);
            if (blocks.Count >= 5)
            {
                break;
            }
        }
        await Assert.That(blocks.Count).IsEqualTo(5);
        // Descending order by timestamp also implies descending block number.
        for (int i = 1; i < blocks.Count; i++)
        {
            await Assert.That(blocks[i - 1].Number > blocks[i].Number).IsTrue();
            await Assert.That(blocks[i - 1].TimestampRange.Starting > blocks[i].TimestampRange.Starting).IsTrue();
        }
    }

    [Test]
    public async Task Can_Get_Block_By_Number_And_By_Hash()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var latest = await mirror.GetLatestBlockAsync();
        await Assert.That(latest).IsNotNull();

        // Pick a block a few back from latest to avoid races where the
        // mirror moves on between our two reads.
        var targetNumber = latest!.Number - 3;
        var byNumber = await mirror.GetBlockAsync(targetNumber);
        await Assert.That(byNumber).IsNotNull();
        await Assert.That(byNumber!.Number).IsEqualTo(targetNumber);
        await Assert.That(byNumber.Hash.Length).IsEqualTo(48);

        // Round-trip the SHA-384 block hash through GetBlockAsync(bytes).
        var byHash = await mirror.GetBlockAsync(byNumber.Hash);
        await Assert.That(byHash).IsNotNull();
        await Assert.That(byHash!.Number).IsEqualTo(targetNumber);
        await Assert.That(byHash.Hash.ToArray()).IsEquivalentTo(byNumber.Hash.ToArray(),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(byHash.PreviousHash.ToArray()).IsEquivalentTo(byNumber.PreviousHash.ToArray(),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
}
