// SPDX-License-Identifier: Apache-2.0
using Proto;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Hiero;
/// <summary>
/// Represents the results returned from a contract call.
/// </summary>
public sealed record ContractCallResult
{
    private static readonly ReadOnlyCollection<ContractEvent> EmptyEvents = Array.AsReadOnly(Array.Empty<ContractEvent>());
    private static readonly ReadOnlyDictionary<EntityId, long> EmptyNonces = new(new Dictionary<EntityId, long>(0));

    /// <summary>
    /// ID of the contract that was called.
    /// </summary>
    public EntityId Contract { get; private init; }
    /// <summary>
    /// The values returned from the contract call.
    /// </summary>
    public EncodedParams Result { get; private init; }
    /// <summary>
    /// The ABI-encoded error or revert data returned by the EVM if the
    /// call failed, empty otherwise.
    /// </summary>
    public EncodedParams Error { get; private init; }
    /// <summary>
    /// Bloom filter aggregating the topics and addresses of every
    /// event emitted by this call.
    /// </summary>
    public ReadOnlyMemory<byte> Bloom { get; private init; }
    /// <summary>
    /// The amount of gas that was used.
    /// </summary>
    public ulong GasUsed { get; private init; }
    /// <summary>
    /// The amount of gas available for the call.
    /// </summary>
    public long GasLimit { get; private init; }
    /// <summary>
    /// Number of tinybars sent into this contract transaction call
    /// (the function must be payable if this is nonzero).
    /// </summary>
    public long PayableAmount { get; private init; }
    /// <summary>
    /// The account that is the "message.sender" of the contract
    /// call, if not present it is the transaction Payer.
    /// </summary>
    public EntityId MessageSender { get; private init; }
    /// <summary>
    /// Log events returned by the function.
    /// </summary>
    public ReadOnlyCollection<ContractEvent> Events { get; private init; }
    /// <summary>
    /// The contract's 20-byte EVM address, may or may not
    /// correspond to the shard.realm.num encoded, an
    /// EIP-1014 derived address or <code>None</code> if not returned
    /// from the network.
    /// </summary>
    public EvmAddress EvmAddress { get; private init; }
    /// <summary>
    /// The encoded selector parameters passed into the contract call.
    /// </summary>
    public EncodedParams Input { get; private init; }
    /// <summary>
    /// A list of updated contract account nonces containing the new nonce 
    /// value for each contract account involved in this transaction. For
    /// query transactions, this should be empty as a contract query call
    /// does not change the state of the EVM.
    /// </summary>
    public ReadOnlyDictionary<EntityId, long> Nonces { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Results
    /// </summary>
    internal ContractCallResult(Response response) : this(response.ContractCallLocal.FunctionResult)
    {
    }
    /// <summary>
    /// Internal Constructor from Raw Results
    /// </summary>
    internal ContractCallResult(ContractFunctionResult result)
    {
        Contract = result.ContractID.AsAddress();
        Result = new EncodedParams(result.ContractCallResult.Memory);
        Error = new EncodedParams(result.ErrorMessage);
        Bloom = result.Bloom.Memory;
        GasUsed = result.GasUsed;
        GasLimit = result.Gas;
        PayableAmount = result.Amount;
        MessageSender = result.SenderId.AsAddress();
        var logs = result.LogInfo;
        var logCount = logs.Count;
        if (logCount == 0)
        {
            Events = EmptyEvents;
        }
        else
        {
            var events = new ContractEvent[logCount];
            for (var i = 0; i < logCount; i++)
            {
                events[i] = new ContractEvent(logs[i]);
            }
            Events = Array.AsReadOnly(events);
        }
        EvmAddress = result.EvmAddress is { Length: 20 } ? new EvmAddress(result.EvmAddress.Memory) : EvmAddress.None;
        Input = new EncodedParams(result.FunctionParameters.Memory);
        var contractNonces = result.ContractNonces;
        var nonceCount = contractNonces?.Count ?? 0;
        if (nonceCount == 0)
        {
            Nonces = EmptyNonces;
        }
        else
        {
            var nonces = new Dictionary<EntityId, long>(nonceCount);
            for (var i = 0; i < nonceCount; i++)
            {
                var nonce = contractNonces![i];
                ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(nonces, nonce.ContractId.AsAddress(), out _);
                value = nonce.Nonce;
            }
            Nonces = new ReadOnlyDictionary<EntityId, long>(nonces);
        }
    }
}
/// <summary>
/// Represents the log events returned by a contract function call.
/// </summary>
public sealed class ContractEvent
{
    /// <summary>
    /// Address of the contract that emitted the event.
    /// </summary>
    public EntityId Contract { get; private init; }
    /// <summary>
    /// Bloom filter for this log record.
    /// </summary>
    public ReadOnlyMemory<byte> Bloom { get; private init; }
    /// <summary>
    /// The indexed topics for this event; the first topic is the
    /// event signature hash, the remainder are the indexed arguments.
    /// </summary>
    public ReadOnlyMemory<byte>[] Topics { get; private init; }
    /// <summary>
    /// The event data returned.
    /// </summary>
    public EncodedParams Data { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Results
    /// </summary>
    internal ContractEvent(ContractLoginfo log)
    {
        Contract = log.ContractID.AsAddress();
        Bloom = log.Bloom.Memory;
        var topics = log.Topic;
        var topicCount = topics.Count;
        if (topicCount == 0)
        {
            Topics = Array.Empty<ReadOnlyMemory<byte>>();
        }
        else
        {
            var topicData = new ReadOnlyMemory<byte>[topicCount];
            for (var i = 0; i < topicCount; i++)
            {
                topicData[i] = topics[i].Memory;
            }
            Topics = topicData;
        }
        Data = new EncodedParams(log.Data.Memory);
    }
}
