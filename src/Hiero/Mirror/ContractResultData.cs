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
    /// The total amount of gas used by all transactions in the block
    /// that contains this result.
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
    /// The gas price of the wrapped ethereum transaction, in tinybars.
    /// </summary>
    [JsonPropertyName("gas_price")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger GasPrice { get; set; }
    /// <summary>
    /// The maximum total fee per gas the sender of a wrapped EIP-1559
    /// (type 2) or later ethereum transaction offered; zero for other
    /// transaction types.
    /// </summary>
    [JsonPropertyName("max_fee_per_gas")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger MaxFeePerGas { get; set; }
    /// <summary>
    /// The maximum priority fee (tip) per gas the sender of a wrapped
    /// EIP-1559 (type 2) or later ethereum transaction offered; zero
    /// for other transaction types.
    /// </summary>
    [JsonPropertyName("max_priority_fee_per_gas")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger MaxPriorityFeePerGas { get; set; }
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
    /// The EVM transaction hash for this result (not the raw HAPI
    /// transaction hash).
    /// </summary>
    [JsonPropertyName("hash")]
    public EvmHash Hash { get; set; } = EvmHash.None;
    /// <summary>
    /// The 48-byte SHA-384 record-file hash of the block the
    /// transaction is included in.
    /// </summary>
    /// <remarks>
    /// Hedera block hashes are SHA-384 outputs (48 bytes / 96 hex
    /// chars on the wire), not 32-byte EVM-style hashes — see the
    /// remarks on <see cref="EvmHash"/>.
    /// </remarks>
    [JsonPropertyName("block_hash")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> BlockHash { get; set; }
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
    /// integer of the transaction type, 0x0 for legacy transactions, 0x1 for access list types, 0x2 for dynamic fees,
    /// 0x4 for EIP-7702 set-code transactions.
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
    /// The HAPI status of the transaction that produced this result.
    /// </summary>
    /// <remarks>
    /// A status the network reports but this build of the SDK does not know
    /// reads as <see cref="ResponseCode.Unrecognized"/> rather than failing
    /// the surrounding query.
    /// </remarks>
    [JsonPropertyName("result")]
    public ResponseCode Status { get; set; }
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
    /// <summary>
    /// The initcode of a contract create that failed; empty otherwise.
    /// </summary>
    [JsonPropertyName("failed_initcode")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> FailedInitcode { get; set; }
    /// <summary>
    /// The EIP-2930 access list of the wrapped ethereum transaction:
    /// the contracts and storage slots it declared up front. Empty for
    /// transactions that declared none, and null from mirror nodes that
    /// predate the structured form (see <see cref="AccessListConverter"/>).
    /// </summary>
    [JsonPropertyName("access_list")]
    [JsonConverter(typeof(AccessListConverter))]
    public AccessListEntryData[]? AccessList { get; set; }
    /// <summary>
    /// The EIP-7702 authorization list of a wrapped set-code (type 4)
    /// ethereum transaction: the delegations it asked the network to
    /// install. Null for other transaction types.
    /// </summary>
    [JsonPropertyName("authorization_list")]
    public EvmAuthorizationData[]? AuthorizationList { get; set; }
    /// <summary>
    /// The contract storage slots the call read or wrote. Reported only
    /// when the result is retrieved individually (by transaction id,
    /// hash or timestamp), not in result listings.
    /// </summary>
    [JsonPropertyName("state_changes")]
    public ContractStateChangeData[]? StateChanges { get; set; }
}
/// <summary>
/// One entry of an EIP-2930 access list: a contract and the storage
/// slots of it the transaction declared it would touch.
/// </summary>
public class AccessListEntryData
{
    /// <summary>
    /// The address of the accessed contract.
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress Address { get; set; } = default!;
    /// <summary>
    /// The accessed storage keys, each 32 bytes.
    /// </summary>
    [JsonPropertyName("storage_keys")]
    [JsonConverter(typeof(HexStringArraytoBytesArrayConverter))]
    public ReadOnlyMemory<byte>[] StorageKeys { get; set; } = [];
}
/// <summary>
/// One entry of an EIP-7702 authorization list: a signed request to
/// delegate the signing account's code to another address.
/// </summary>
public class EvmAuthorizationData
{
    /// <summary>
    /// The address the account's code is delegated to.
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress Address { get; set; } = default!;
    /// <summary>
    /// The chain the authorization is valid on; zero for any chain.
    /// </summary>
    [JsonPropertyName("chain_id")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger ChainId { get; set; }
    /// <summary>
    /// The signing account's nonce the authorization is bound to.
    /// </summary>
    [JsonPropertyName("nonce")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Nonce { get; set; }
    /// <summary>
    /// The r value of the authorization signature.
    /// </summary>
    [JsonPropertyName("r")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger SignatureR { get; set; }
    /// <summary>
    /// The s value of the authorization signature.
    /// </summary>
    [JsonPropertyName("s")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger SignatureS { get; set; }
    /// <summary>
    /// The y-parity (recovery id) of the authorization signature, 0 or
    /// 1. Hex encoded on the wire like the other signature values.
    /// </summary>
    [JsonPropertyName("y_parity")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger YParity { get; set; }
}
/// <summary>
/// A contract storage slot a call read, and possibly wrote.
/// </summary>
public class ContractStateChangeData
{
    /// <summary>
    /// The EVM address of the contract owning the slot.
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress Address { get; set; } = default!;
    /// <summary>
    /// The HAPI entity id of the contract owning the slot.
    /// </summary>
    [JsonPropertyName("contract_id")]
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// The storage slot.
    /// </summary>
    [JsonPropertyName("slot")]
    [JsonConverter(typeof(BigIntegerConverter))]
    public BigInteger Slot { get; set; }
    /// <summary>
    /// The value the call read from the slot.
    /// </summary>
    [JsonPropertyName("value_read")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> ValueRead { get; set; }
    /// <summary>
    /// The value the call wrote to the slot; empty when it only read.
    /// </summary>
    [JsonPropertyName("value_written")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> ValueWritten { get; set; }
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
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
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
    public static IAsyncEnumerable<ContractResultData> GetContractResultsAsync(this MirrorRestClient client, EntityId contract, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{contract.ToMirrorString()}/results", new PageLimit(100), filters);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path, MirrorJsonContext.Default.ContractResultDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetContractResultsAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<ContractResultData> GetContractResultsAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryParameter[] filters)
    {
        return client.GetContractResultsAsync(contract, CancellationToken.None, filters);
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
    /// <param name="cancellationToken">
    /// Token that cancels the request.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="HbarTransferProjectionFilter"/> to opt out of the
    /// HBAR-transfer subtree on the returned record.
    /// </param>
    /// <returns>
    /// The contract result, or null if not found.
    /// </returns>
    public static Task<ContractResultData?> GetContractResultByTimestampAsync(this MirrorRestClient client, EntityId contract, ConsensusTimeStamp timestamp, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{contract.ToMirrorString()}/results/{timestamp}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultData, cancellationToken);
    }
    /// <inheritdoc cref="GetContractResultByTimestampAsync(MirrorRestClient, EntityId, ConsensusTimeStamp, CancellationToken, IMirrorQueryParameter[])"/>
    public static Task<ContractResultData?> GetContractResultByTimestampAsync(this MirrorRestClient client, EntityId contract, ConsensusTimeStamp timestamp, params IMirrorQueryParameter[] filters)
    {
        return client.GetContractResultByTimestampAsync(contract, timestamp, CancellationToken.None, filters);
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
    /// <param name="cancellationToken">
    /// Token that cancels the request.
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
    public static Task<ContractResultData?> GetContractResultByTransactionHashAsync(this MirrorRestClient client, EvmHash evmTransactionHash, CancellationToken cancellationToken, params IMirrorProjection[] filters)
    {
        var path = GenerateInitialPath($"contracts/results/{evmTransactionHash}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultData, cancellationToken);
    }
    /// <inheritdoc cref="GetContractResultByTransactionHashAsync(MirrorRestClient, EvmHash, CancellationToken, IMirrorProjection[])"/>
    public static Task<ContractResultData?> GetContractResultByTransactionHashAsync(this MirrorRestClient client, EvmHash evmTransactionHash, params IMirrorProjection[] filters)
    {
        return client.GetContractResultByTransactionHashAsync(evmTransactionHash, CancellationToken.None, filters);
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
    /// <param name="cancellationToken">
    /// Token that cancels the request.
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
    /// <exception cref="ArgumentException">
    /// If the transaction id is scheduled: this mirror node route cannot
    /// select a scheduled transaction.
    /// </exception>
    public static Task<ContractResultData?> GetContractResultByTransactionIdAsync(this MirrorRestClient client, TransactionId transactionId, CancellationToken cancellationToken, params IMirrorProjection[] filters)
    {
        var (txId, txFilters) = MirrorFormatForContractResult(transactionId, nonceSupported: true, nameof(transactionId));
        var path = GenerateInitialPath($"contracts/results/{txId}", txFilters, filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultData, cancellationToken);
    }
    /// <inheritdoc cref="GetContractResultByTransactionIdAsync(MirrorRestClient, TransactionId, CancellationToken, IMirrorProjection[])"/>
    public static Task<ContractResultData?> GetContractResultByTransactionIdAsync(this MirrorRestClient client, TransactionId transactionId, params IMirrorProjection[] filters)
    {
        return client.GetContractResultByTransactionIdAsync(transactionId, CancellationToken.None, filters);
    }
    /// <summary>
    /// Retrieves a single contract-call result by its position within a
    /// block, via
    /// <c>/api/v1/contracts/results?block.hash={hash}&amp;transaction.index={position}</c>.
    /// The server returns a list and this method unwraps the first
    /// item — a matching result should be unique for a given (block,
    /// position) pair.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockHash">
    /// The block hash bytes — accepts the 48-byte Hedera SHA-384
    /// record-file hash returned by the mirror, or a 32-byte EVM
    /// block hash for callers that have one.
    /// </param>
    /// <param name="position">
    /// The transaction position within the block.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the request.
    /// </param>
    /// <returns>
    /// The contract results data or null if not found.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="blockHash"/> is neither 32 nor 48 bytes long.
    /// </exception>
    public static async Task<ContractResultData?> GetContractResultByBlockAndPositionAsync(this MirrorRestClient client, ReadOnlyMemory<byte> blockHash, long position, CancellationToken cancellationToken = default)
    {
        if (blockHash.Length != 32 && blockHash.Length != 48)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHash), "Block hash must be 32 bytes (EVM) or 48 bytes (Hedera SHA-384).");
        }
        var path = GenerateInitialPath("contracts/results", BlockHashFilter.Is(blockHash), TransactionIndexFilter.Is(position));
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractResultDataPage, cancellationToken).ConfigureAwait(false);
        var results = list?.Results;
        return results is { Length: > 0 } ? results[0] : null;
    }
    /// <summary>
    /// Enumerates every contract-call result contained in a block,
    /// via <c>/api/v1/contracts/results?block.hash={hash}</c>. Results
    /// are returned in ascending transaction-index order.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockHash">
    /// The block hash bytes — accepts the 48-byte Hedera SHA-384
    /// record-file hash returned by the mirror, or a 32-byte EVM
    /// block hash for callers that have one.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
    /// </param>
    /// <returns>
    /// An async enumerable of contract-call results in the block; may
    /// be empty if the block contained no contract calls.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="blockHash"/> is neither 32 nor 48 bytes long.
    /// </exception>
    public static IAsyncEnumerable<ContractResultData> GetContractResultsByBlockHashAsync(this MirrorRestClient client, ReadOnlyMemory<byte> blockHash, CancellationToken cancellationToken = default)
    {
        if (blockHash.Length != 32 && blockHash.Length != 48)
        {
            throw new ArgumentOutOfRangeException(nameof(blockHash), "Block hash must be 32 bytes (EVM) or 48 bytes (Hedera SHA-384).");
        }
        var path = GenerateInitialPath("contracts/results", BlockHashFilter.Is(blockHash), new PageLimit(100), OrderBy.Ascending);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path, MirrorJsonContext.Default.ContractResultDataPage, cancellationToken);
    }
    /// <summary>
    /// Enumerates contract-call results across every contract on the
    /// network from <c>/api/v1/contracts/results</c>. Same filter
    /// palette as <see cref="GetContractResultsAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>, without the
    /// per-contract scoping, plus the
    /// <see cref="HbarTransferProjectionFilter"/> toggle on the
    /// HBAR-transfer subtree.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
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
    public static IAsyncEnumerable<ContractResultData> GetAllContractResultsAsync(this MirrorRestClient client, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("contracts/results", new PageLimit(100), OrderBy.Ascending, filters);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path, MirrorJsonContext.Default.ContractResultDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetAllContractResultsAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<ContractResultData> GetAllContractResultsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        return client.GetAllContractResultsAsync(CancellationToken.None, filters);
    }
    /// <summary>
    /// Retrieves the EVM chain id (EIP-155) that this mirror's network uses.
    /// </summary>
    /// <remarks>
    /// The mirror node has no dedicated chain-id endpoint: <c>chain_id</c> is
    /// only populated on contract results that wrapped an
    /// <c>EthereumTransaction</c> — HAPI-native contract calls report it as
    /// zero. This method therefore locates the most recent Ethereum-wrapped
    /// transaction (<c>/api/v1/transactions?transactiontype=ethereumtransaction</c>)
    /// and reads the chain id from its contract result, which is reliable across
    /// the network's whole history rather than just a recent window. It remains
    /// best-effort: a network that has never processed an <c>EthereumTransaction</c>
    /// (e.g. a freshly-started local network) exposes the value nowhere, and the
    /// method throws rather than returning a fabricated value — a wrong or zero
    /// chain id would sign a transaction the network later rejects with
    /// <see cref="ResponseCode.WrongChainId"/>. Chain id is static network
    /// configuration, so callers should cache the result rather than re-derive
    /// it per call, or supply a known value directly.
    /// </remarks>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the request.
    /// </param>
    /// <returns>
    /// The Chain ID of the network.
    /// </returns>
    /// <exception cref="MirrorException">
    /// Thrown when the mirror node holds no Ethereum-wrapped transaction from
    /// which to read the chain id (typically a network with no EVM/relay traffic
    /// yet, such as a freshly-started local network), or when the contract
    /// result of the most recent one is unavailable or carries no chain id.
    /// </exception>
    public static async Task<BigInteger> GetChainIdAsync(this MirrorRestClient client, CancellationToken cancellationToken = default)
    {
        await foreach (var transaction in client.GetTransactionsAsync(cancellationToken, TransactionTypeFilter.EthereumTransaction, OrderBy.Descending).ConfigureAwait(false))
        {
            var result = await client.GetContractResultByTransactionIdAsync(transaction.TransactionId, cancellationToken).ConfigureAwait(false);
            if (result is not null && result.ChainId != BigInteger.Zero)
            {
                return result.ChainId;
            }
            // Only the most recent Ethereum transaction is needed; if its result
            // is unavailable or lacks a chain id, fall through and fail loudly.
            break;
        }
        throw new MirrorException("No Ethereum-wrapped transaction found on the mirror node; unable to determine the network Chain ID.", [], System.Net.HttpStatusCode.NotFound);
    }
}
