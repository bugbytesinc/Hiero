// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Numerics;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents the results from an EVM contract call.
/// </summary>
public class ContractResultData
{
    /// <summary>
    /// The EVM address of the contract that was called
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress ContractAddress { get; set; } = default!;
    /// <summary>
    /// Number of tinybars sent into this contract transaction call
    /// (the function must be payable if this is nonzero).
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long PayableAmount { get; set; }
    /// <summary>
    /// Bloom filter for record
    /// </summary>
    [JsonPropertyName("bloom")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Bloom { get; set; }
    /// <summary>
    /// The values returned from the contract call.
    /// </summary>
    [JsonPropertyName("call_result")]
    public EncodedParams Result { get; set; } = default!;
    /// <summary>
    /// ID of the contract that was called.
    /// </summary>
    [JsonPropertyName("contract_id")]
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// IDs of any contracts that were created as
    /// a side effect of this contract call.
    /// </summary>
    [JsonPropertyName("created_contract_ids")]
    [JsonConverter(typeof(EntityIdArrayConverter))]
    public EntityId[] CreatedContracts { get; set; } = default!;
    /// <summary>
    /// The Address or contract that is msg.sender for
    /// this contract call.
    /// </summary>
    [JsonPropertyName("from")]
    public EntityId MessageSender { get; set; } = default!;
    /// <summary>
    /// The input function parameters for the call
    /// </summary>
    [JsonPropertyName("function_parameters")]
    public EncodedParams Input { get; set; } = default!;
    /// <summary>
    /// The units of consumed gas by the EVM to execute contract, 
    /// which may be less than the amount of gas used.
    /// </summary>
    [JsonPropertyName("gas_consumed")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong GasConsumed { get; set; } = default!;
    /// <summary>
    /// The amount of gas that was debted (charged)
    /// </summary>
    [JsonPropertyName("block_gas_used")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong BlockGasUsed { get; set; }
    /// <summary>
    /// The units of gas charged by the network 
    /// to execute contract, may be more than the
    /// minimum amount consumed by the EVM itself.
    /// </summary>
    [JsonPropertyName("gas_used")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong GasUsed { get; set; }
    /// <summary>
    /// The maximum units of gas allowed for contract execution.
    /// </summary>
    [JsonPropertyName("gas_limit")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long GasLimit { get; set; }
    /// <summary>
    /// Gas price (not sure of denomination)
    /// </summary>
    [JsonPropertyName("gas_price")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger GasPrice { get; set; }
    /// <summary>
    /// The transaction’s consensus timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Consensus { get; set; }
    /// <summary>
    /// The Contract TransactionId's TO parameter
    /// (should be same as Contract)
    /// </summary>
    [JsonPropertyName("to")]
    public EntityId MessageReceiver { get; set; } = default!;
    /// <summary>
    /// The Hash of the TransactionId
    /// </summary>
    [JsonPropertyName("hash")]
    public EvmHash Hash { get; set; } = EvmHash.None;
    /// <summary>
    /// The Hash of the block the transaction is included in.
    /// </summary>
    [JsonPropertyName("block_hash")]
    public EvmHash BlockHash { get; set; } = EvmHash.None;
    /// <summary>
    /// The number of the block the transaction was in.
    /// </summary>
    [JsonPropertyName("block_number")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long BlockNumber { get; set; }
    /// <summary>
    /// The nonce of the wrapped ethereum transaction
    /// </summary>
    [JsonPropertyName("nonce")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong Nonce { get; set; }
    /// <summary>
    /// The position of the transaction in the block.
    /// </summary>
    [JsonPropertyName("transaction_index")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong TransactionIndex { get; set; }
    /// <summary>
    /// The signature_r of the wrapped ethereum transaction
    /// </summary>
    [JsonPropertyName("r")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger SignatureR { get; set; }
    /// <summary>
    /// The signature_s of the wrapped ethereum transaction
    /// </summary>
    [JsonPropertyName("s")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger SignatureS { get; set; }
    /// <summary>
    /// The recovery_id of the wrapped ethereum transaction.
    /// </summary>
    [JsonPropertyName("v")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long SignatureV { get; set; }
    /// <summary>
    /// List of logs.
    /// </summary>
    [JsonPropertyName("logs")]
    public ContractLogData[]? Logs { get; set; }
    /// <summary>
    /// integer of the transaction type, 0x0 for legacy transactions, 0x1 for access list types, 0x2 for dynamic fees.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int TransactionType { get; set; }
    /// <summary>
    ///  either 1 (success) or 0 (failure)
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger TransactionStatus { get; set; }
    /// <summary>
    /// Chain ID
    /// </summary>
    [JsonPropertyName("chain_id")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger ChainId { get; set; }
    /// <summary>
    /// Error Message if one exists, can be a revert encoded
    /// hex value or actually just a string depending on how
    /// the error was raised from the contract.
    /// </summary>
    [JsonPropertyName("error_message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; set; }
}
/// <summary>
/// Extension methods for querying contract execution results from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractResultDataExtensions
{
    /// <summary>
    /// Enumerates calls made to a specific contract from
    /// <c>/api/v1/contracts/{id}/results</c>, regardless of how the
    /// call was routed (HAPI or JSON-RPC). Use
    /// <see cref="TimestampFilter"/> or <see cref="BlockNumberFilter"/>
    /// to bracket a range, <see cref="EvmSenderFilter"/> to narrow by
    /// EVM caller, or <see cref="InternalProjectionFilter"/> to include
    /// child-transaction calls. Newest-first by default; pass
    /// <see cref="OrderBy.Ascending"/> to reverse.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The entityId of the contract
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="BlockHashFilter"/>, <see cref="BlockNumberFilter"/>,
    /// <see cref="EvmSenderFilter"/>,
    /// <see cref="InternalProjectionFilter"/>,
    /// <see cref="TimestampFilter"/>,
    /// <see cref="TransactionIndexFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of contract-call records. Each record is a
    /// lightweight summary; the full HAPI transaction details require
    /// a separate <c>GetTransactionAsync</c> lookup by consensus
    /// timestamp.
    /// </returns>
    public static IAsyncEnumerable<ContractResultData> GetContractResultsAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/results", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path, MirrorJsonContext.Default.ContractResultDataPage);
    }
    /// <summary>
    /// Retrieves the single contract result produced by the given
    /// contract at the given consensus timestamp — useful when the
    /// timestamp is already known (e.g., from a transaction record
    /// or a block listing) and a txid/hash lookup is not available.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The contract whose result is requested.
    /// </param>
    /// <param name="timestamp">
    /// The consensus timestamp identifying the execution.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="HbarTransferProjectionFilter"/> to opt out of the
    /// HBAR-transfer subtree on the returned record.
    /// </param>
    /// <returns>
    /// The contract result, or null if not found.
    /// </returns>
    public static Task<ContractResultData?> GetContractResultByTimestampAsync(this MirrorRestClient client, EntityId contract, ConsensusTimeStamp timestamp, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/results/{timestamp}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultData);
    }
    /// <summary>
    /// Retrieve the contract results for a specific transaction by hash.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="evmTransactionHash">
    /// The EVM TransactionId hash (not to be confused with the raw HAPI transaction hash)
    /// </param>
    /// <param name="filters">
    /// Optional projection toggles recognized by this endpoint —
    /// currently <see cref="HbarTransferProjectionFilter"/>, which
    /// controls whether the HBAR-transfer subtree is included in
    /// the returned record.
    /// </param>
    /// <returns>
    /// The contract results data, or null if not found.
    /// </returns>
    public static Task<ContractResultData?> GetContractResultByTransactionHashAsync(this MirrorRestClient client, EvmHash evmTransactionHash, params IMirrorProjection[] filters)
    {
        var path = GenerateInitialPath($"contracts/results/{evmTransactionHash}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultData);
    }
    /// <summary>
    /// Retrieve the contract results for a specific transaction by HAPI transaction ID
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="transactionId">
    /// The HAPI transaction id, not the EVM transaction hash
    /// </param>
    /// <param name="filters">
    /// Optional projection toggles recognized by this endpoint —
    /// currently <see cref="HbarTransferProjectionFilter"/>, which
    /// controls whether the HBAR-transfer subtree is included in
    /// the returned record.
    /// </param>
    /// <returns>
    /// The contract results data or null if not found
    /// </returns>
    public static async Task<ContractResultData?> GetContractResultByTransactionIdAsync(this MirrorRestClient client, TransactionId transactionId, params IMirrorProjection[] filters)
    {
        var (txId, txFilters) = MirrorFormat(transactionId);
        var path = GenerateInitialPath($"contracts/results/{txId}", [.. txFilters, .. filters]);
        return await client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultData);
    }
    /// <summary>
    /// Retrieves a single contract-call result by its position within a
    /// simulated EVM block, via
    /// <c>/api/v1/contracts/results?block.hash={hash}&amp;transaction.index={position}</c>.
    /// The server returns a list and this method unwraps the first
    /// item — a matching result should be unique for a given (block,
    /// position) pair.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockHash">
    /// The Simulated EVM Block hash.
    /// </param>
    /// <param name="position">
    /// The transaction position within the simulated EVM Block.
    /// </param>
    /// <returns>
    /// The contract results data or null if not found.
    /// </returns>
    public static async Task<ContractResultData?> GetContractResultByBlockAndPositionAsync(this MirrorRestClient client, EvmHash blockHash, long position)
    {
        var path = $"contracts/results?block.hash={blockHash}&transaction.index={position}";
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultDataPage).ConfigureAwait(false);
        return list?.Results?.FirstOrDefault();
    }
    /// <summary>
    /// Enumerates every contract-call result contained in a simulated
    /// EVM block, via
    /// <c>/api/v1/contracts/results?block.hash={hash}</c>. Results are
    /// returned in ascending transaction-index order.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockHash">
    /// Hash of the simulated EVM block.
    /// </param>
    /// <returns>
    /// An async enumerable of contract-call results in the block; may
    /// be empty if the block contained no contract calls.
    /// </returns>
    public static IAsyncEnumerable<ContractResultData> GetContractResultsByBlockHashAsync(this MirrorRestClient client, EvmHash blockHash)
    {
        var path = $"contracts/results?block.hash={blockHash}&limit=100&order=asc";
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path, MirrorJsonContext.Default.ContractResultDataPage);
    }
    /// <summary>
    /// Enumerates contract-call results across every contract on the
    /// network from <c>/api/v1/contracts/results</c>. Same filter
    /// palette as <see cref="GetContractResultsAsync"/>, without the
    /// per-contract scoping, plus the
    /// <see cref="HbarTransferProjectionFilter"/> toggle on the
    /// HBAR-transfer subtree.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="BlockHashFilter"/>, <see cref="BlockNumberFilter"/>,
    /// <see cref="EvmSenderFilter"/>,
    /// <see cref="HbarTransferProjectionFilter"/>,
    /// <see cref="InternalProjectionFilter"/>,
    /// <see cref="TimestampFilter"/>,
    /// <see cref="TransactionIndexFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of contract-call records. Empty when no
    /// results match the supplied criteria.
    /// </returns>
    public static IAsyncEnumerable<ContractResultData> GetAllContractResultsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/results", [new PageLimit(100), OrderBy.Ascending, .. filters]);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path, MirrorJsonContext.Default.ContractResultDataPage);
    }
    /// <summary>
    /// Retrieves the EVM chain id that this mirror's network uses,
    /// derived by scanning recent results from
    /// <c>/api/v1/contracts/results</c> for the first record that
    /// carries a non-zero chain id. There is no dedicated chain-id
    /// endpoint on the mirror node.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The Chain ID of the Hedera network, or zero if no record in the
    /// scanned page had one set.
    /// </returns>
    /// <exception cref="MirrorException">
    /// Thrown when the mirror node has no contract-result records to
    /// scan (typically a freshly-started local network).
    /// </exception>
    public static async Task<BigInteger> GetChainIdAsync(this MirrorRestClient client)
    {
        var path = GenerateInitialPath($"contracts/results", [new PageLimit(10), OrderBy.Descending]);
        var data = (await client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultDataPage).ConfigureAwait(false))?.Results ?? throw new MirrorException("Contract results are empty, unable to find Chain ID.", [], System.Net.HttpStatusCode.NotFound);
        for (int i = 0; i < data.Length; i++)
        {
            var result = data[i];
            if (result.ChainId != BigInteger.Zero)
            {
                return result.ChainId;
            }
        }
        throw new MirrorException("Chain ID not found in contract results.", [], System.Net.HttpStatusCode.NotFound);
    }
}