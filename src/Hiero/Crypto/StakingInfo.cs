// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;
/// <summary>
/// The staking information returned from the
/// CryptoGetInfo or ContractGetInfo queries.
/// </summary>
public sealed record StakingInfo
{
    /// <summary>
    /// If true, the account or contract has declined
    /// to receive a staking reward
    /// </summary>
    public bool Declined { get; private init; }
    /// <summary>
    /// The timestamp at which the most recent reward was 
    /// earned or the staking configuration for this entity 
    /// was updated, whichever is later.
    /// </summary>
    public ConsensusTimeStamp PeriodStart { get; private init; }
    /// <summary>
    /// The pending amount of tinybars that will be received
    /// at the next reward payout.
    /// </summary>
    public long PendingReward { get; private init; }
    /// <summary>
    /// The total number of tinybars that are proxy staked 
    /// to this account or contract.
    /// </summary>
    public long Proxied { get; private init; }
    /// <summary>
    /// The address of the account or contract that this
    /// account or contract is proxy staking to, or 
    /// <code>None</code> if it is directly staking to
    /// a Hedera node.
    /// </summary>
    public EntityId Proxy { get; private init; }
    /// <summary>
    /// The ID of the Hedera node this account or contract
    /// is directly staking to.  A value of 0 is ambiguous:
    /// it is reported for staking to node 0 and also when
    /// this account or contract is not staking to a node
    /// (proxy staking, or not staking at all).  Check
    /// <see cref="Proxy"/> to detect proxy staking.
    /// </summary>
    public long Node { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Response
    /// </summary>
    internal StakingInfo(Proto.StakingInfo info)
    {
        if (info is null)
        {
            Declined = false;
            PeriodStart = ConsensusTimeStamp.MinValue;
            PendingReward = 0;
            Proxied = 0;
            Proxy = EntityId.None;
            Node = 0;
        }
        else
        {
            Declined = info.DeclineReward;
            PeriodStart = info.StakePeriodStart?.ToConsensusTimeStamp() ?? ConsensusTimeStamp.MinValue;
            PendingReward = info.PendingReward;
            Proxied = info.StakedToMe;
            Proxy = info.StakedAccountId.AsAddress();
            Node = info.StakedNodeId;
        }
    }
}