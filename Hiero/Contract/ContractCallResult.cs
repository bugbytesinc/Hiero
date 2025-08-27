﻿using Proto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hiero;
/// <summary>
/// Represents the results returned from a contract call.
/// </summary>
public sealed record ContractCallResult
{
    /// <summary>
    /// ID of the contract that was called.
    /// </summary>
    public EntityId Contract { get; private init; }
    /// <summary>
    /// The values returned from the contract call.
    /// </summary>
    public EncodedParams Result { get; private init; }
    /// <summary>
    /// An error returned from the system if there was a problem.
    /// </summary>
    public EncodedParams Error { get; private init; }
    /// <summary>
    /// Bloom filter for record
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
    /// The contract's 20-byte EVM address, may or may 
    /// correspond to the shard.realm.num encoded, the
    /// EIP-1014 or <code>None</code> if not returned 
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
    /// Query transactions, this should be empty as a Contract Query call
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
        Bloom = result.Bloom.ToArray();
        GasUsed = result.GasUsed;
        GasLimit = result.Gas;
        PayableAmount = result.Amount;
        MessageSender = result.SenderId.AsAddress();
        Events = result.LogInfo.Select(log => new ContractEvent(log)).ToList().AsReadOnly();
        EvmAddress = result.EvmAddress is { Length: 20 } ? new EvmAddress(result.EvmAddress.Memory) : EvmAddress.None;
        Input = new EncodedParams(result.FunctionParameters.Memory);
        Nonces = new ReadOnlyDictionary<EntityId, long>(result.ContractNonces?.ToDictionary(i => i.ContractId.AsAddress(), i => i.Nonce) ?? new Dictionary<EntityId, long>());
    }
}
/// <summary>
/// Represents the log events returned by a contract function call.
/// </summary>
public sealed class ContractEvent
{
    /// <summary>
    /// Payer of a contract that emitted the event.
    /// </summary>
    public EntityId Contract { get; private init; }
    /// <summary>
    /// Bloom filter for this log record.
    /// </summary>
    public ReadOnlyMemory<byte> Bloom { get; private init; }
    /// <summary>
    /// Associated Event Topics
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
        Bloom = log.Bloom.ToArray();
        Topics = log.Topic.Select(bs => new ReadOnlyMemory<byte>(bs.ToArray())).ToArray();
        Data = new EncodedParams(log.Data.ToArray());
    }
}