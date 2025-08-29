using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// The information returned from the GetTopicInfo ConsensusClient 
/// method call.  It represents the details concerning a 
/// Hedera Network Consensus Topics.
/// </summary>
public sealed record TopicInfo
{
    /// <summary>
    /// The memo associated with the topic instance.
    /// </summary>
    public string Memo { get; private init; }
    /// <summary>
    /// A SHA-384 Running Hash of the following: Previous RunningHash,
    /// TopicId, ConsensusTimestamp, SequenceNumber and Message
    /// </summary>
    public ReadOnlyMemory<byte> RunningHash { get; private init; }
    /// <summary>
    /// The number of Messages submitted to this topic at the
    /// time of the call to Get Topics Info.
    /// </summary>
    public ulong SequenceNumber { get; private init; }
    /// <summary>
    /// The Consensus Time after which this topic will no longer accept
    /// messages.  The topic will automatically be deleted after
    /// the system defined grace period beyond the expiration time.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// An endorsement, when specified, can be used to 
    /// authorize a modification or deletion of this topic, including
    /// control of topic lifetime extensions. Additionally, if null, 
    /// any account can extend the topic lifetime.
    /// </summary>
    public Endorsement? Administrator { get; private init; }
    /// <summary>
    /// Identifies the key requirements for submitting messages to this topic.
    /// If blank, any account may submit messages to this topic, 
    /// otherwise they must meet the specified signing requirements.
    /// </summary>
    public Endorsement? Participant { get; private init; }
    /// <summary>
    /// Incremental period for auto-renewal of the topic. If
    /// auto-renew account does not have sufficient funds to renew 
    /// at the expiration time, it will be renewed for a period of 
    /// time the remaining funds can support.  If no funds remain, 
    /// the topic will be deleted.
    /// </summary>
    public TimeSpan AutoRenewPeriod { get; private init; }
    /// <summary>
    /// Payer of the account supporting the auto renewal of 
    /// the topic at expiration time.  The topic lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.
    /// </summary>
    public EntityId? RenewAccount { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this topic information
    /// was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// <summary>
    /// Internal constructor from raw response.
    /// </summary>
    internal TopicInfo(Response response)
    {
        var info = response.ConsensusGetTopicInfo.TopicInfo;
        Memo = info.Memo;
        RunningHash = info.RunningHash.ToArray();
        SequenceNumber = info.SequenceNumber;
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        Administrator = info.AdminKey?.ToEndorsement();
        Participant = info.SubmitKey?.ToEndorsement();
        AutoRenewPeriod = info.AutoRenewPeriod.ToTimeSpan();
        RenewAccount = info.AutoRenewAccount?.AsAddress();
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TopicInfoExtensions
{
    /// <summary>
    /// Retrieves detailed information regarding a Topics Instance.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="topic">
    /// The Hedera Network Payer of the Topics instance to retrieve.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the contract instance.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<TopicInfo> GetTopicInfoAsync(this ConsensusClient client, EntityId topic, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new TopicInfo(await Engine.QueryAsync(client, new ConsensusGetTopicInfoQuery { TopicID = new TopicID(topic) }, cancellationToken, configure).ConfigureAwait(false));
    }
}