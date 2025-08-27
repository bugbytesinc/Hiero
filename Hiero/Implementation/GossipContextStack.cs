using Google.Protobuf;
using Grpc.Net.Client;
using System;

namespace Hiero.Implementation;

/// <summary>
/// Internal Implementation of the <see cref="IConsensusContext"/> used for configuring
/// <see cref="ConsensusClient"/> objects.  Maintains a stack of parent objects 
/// and coordinates values returned for various contexts.  Not intended for
/// public use.
/// </summary>
internal class GossipContextStack : ContextStack<GossipContextStack, ConsensusNodeEndpoint>, IConsensusContext
{
    public ConsensusNodeEndpoint? Endpoint { get => get<ConsensusNodeEndpoint>(nameof(Endpoint)); set => set(nameof(Endpoint), value); }
    public EntityId? Payer { get => get<EntityId>(nameof(Payer)); set => set(nameof(Payer), value); }
    public Signatory? Signatory { get => get<Signatory>(nameof(Signatory)); set => set(nameof(Signatory), value); }
    public long FeeLimit { get => get<long>(nameof(FeeLimit)); set => set(nameof(FeeLimit), value); }
    public TimeSpan TransactionDuration { get => get<TimeSpan>(nameof(TransactionDuration)); set => set(nameof(TransactionDuration), value); }
    public int RetryCount { get => get<int>(nameof(RetryCount)); set => set(nameof(RetryCount), value); }
    public TimeSpan RetryDelay { get => get<TimeSpan>(nameof(RetryDelay)); set => set(nameof(RetryDelay), value); }
    public long QueryTip { get => get<long>(nameof(QueryTip)); set => set(nameof(QueryTip), value); }
    public int SignaturePrefixTrimLimit { get => get<int>(nameof(SignaturePrefixTrimLimit)); set => set(nameof(SignaturePrefixTrimLimit), value); }
    public string? Memo { get => get<string>(nameof(Memo)); set => set(nameof(Memo), value); }
    public bool AdjustForLocalClockDrift { get => get<bool>(nameof(AdjustForLocalClockDrift)); set => set(nameof(AdjustForLocalClockDrift), value); }
    public bool ThrowIfNotSuccess { get => get<bool>(nameof(ThrowIfNotSuccess)); set => set(nameof(ThrowIfNotSuccess), value); }
    public TransactionId? TransactionId { get => get<TransactionId>(nameof(TransactionId)); set => set(nameof(TransactionId), value); }
    public Action<IMessage>? OnSendingRequest { get => get<Action<IMessage>>(nameof(OnSendingRequest)); set => set(nameof(OnSendingRequest), value); }
    public Action<int, IMessage>? OnResponseReceived { get => get<Action<int, IMessage>>(nameof(OnResponseReceived)); set => set(nameof(OnResponseReceived), value); }

    public GossipContextStack(GossipContextStack parent) : base(parent) { }
    public GossipContextStack(Func<ConsensusNodeEndpoint, GrpcChannel> channelFactory) : base(channelFactory) { }
    protected override bool IsValidPropertyName(string name)
    {
        switch (name)
        {
            case nameof(Endpoint):
            case nameof(Payer):
            case nameof(Signatory):
            case nameof(FeeLimit):
            case nameof(RetryCount):
            case nameof(RetryDelay):
            case nameof(QueryTip):
            case nameof(SignaturePrefixTrimLimit):
            case nameof(TransactionDuration):
            case nameof(Memo):
            case nameof(AdjustForLocalClockDrift):
            case nameof(ThrowIfNotSuccess):
            case nameof(TransactionId):
            case nameof(OnSendingRequest):
            case nameof(OnResponseReceived):
                return true;
            default:
                return false;
        }
    }
    protected override ConsensusNodeEndpoint GetChannelKey()
    {
        return Endpoint ?? throw new InvalidOperationException("The Network Consensus Endpoint has not been configured.");
    }
}