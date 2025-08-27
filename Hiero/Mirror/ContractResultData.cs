using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents the results from an EVM contract call.
/// </summary>
public class ContractResultData
{
    /// <summary>
    /// Payer of the contract that was called
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress ContractAddresss { get; set; } = default!;
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
    /// a side affect of this contract call.
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
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Hash { get; set; }
    /// <summary>
    /// The Hash of the block the transaction is included in.
    /// </summary>
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractResultDataExtensions
{
    /// <summary>
    /// Retrieves a list of calls to a contract, regardless of how
    /// the call was routed to the contract (HAPI or JSON-RPC)
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The entityId of the contract
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// A list of contract results data for each contract call, the
    /// returned data is not comprehensive in that it does not include
    /// all of the assoiated HAPI transaction data, to retrieve that
    /// data, additional calls retrieveing transaction data may be required.
    /// </returns>
    public static IAsyncEnumerable<ContractResultData> GetResultsForContractAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/results", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path);
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
    /// <returns>
    /// The contract results data, or null if not found.
    /// </returns>
    public static Task<ContractResultData?> GetContractResultsFromTransactionHashAsync(this MirrorRestClient client, ReadOnlyMemory<byte> evmTransactionHash)
    {
        return client.GetSingleItemAsync<ContractResultData>($"contracts/results/0x{Hex.FromBytes(evmTransactionHash)}");
    }
    /// <summary>
    /// Retrieve the contract results for a specific transaction by HAPI transaction ID
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="transactionId">
    /// The HAPI transaction id, not teh EVM transaction hash
    /// </param>
    /// <returns>
    /// The contract results data or null if not found
    /// </returns>
    public static async Task<ContractResultData?> GetContractResultsFromTxIdAsync(this MirrorRestClient client, TransactionId transactionId)
    {
        return await client.GetSingleItemAsync<ContractResultData>($"contracts/results/{MirrorFormat(transactionId)}");
    }
    /// <summary>
    /// Retrieve the contract results for a specific transaction by simulated block and position
    /// within that block.
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
    public static async Task<ContractResultData?> GetContractResultsFromBlockAndPosition(this MirrorRestClient client, ReadOnlyMemory<byte> blockHash, long position)
    {
        var path = $"contracts/results?block.hash=0x{Hex.FromBytes(blockHash)}&transaction.index={position}";
        var list = await client.GetSingleItemAsync<ContractResultDataPage>(path).ConfigureAwait(false);
        return list?.Results?.FirstOrDefault();
    }
    /// <summary>
    /// Retrieves the list of contract results contained within the simulated EVM Block
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockHash">
    /// Hash of the simulated EVM block.
    /// </param>
    /// <returns>
    /// Enumerator of contract results data, can be an empty list.
    /// </returns>
    public static IAsyncEnumerable<ContractResultData> GetContractResultsFromBlockHashAsync(this MirrorRestClient client, ReadOnlyMemory<byte> blockHash)
    {
        var path = $"contracts/results?block.hash=0x{Hex.FromBytes(blockHash)}&limit=100&order=asc";
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path);
    }
    /// <summary>
    /// Retrieves the list of contract results fulfilling the fitlered criteria
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Optional Set of Filters to Apply
    /// </param>
    /// <returns>
    /// Enumerator of contract results data, can be an empty list.
    /// </returns>
    public static IAsyncEnumerable<ContractResultData> GetAllContractResultsAsync(this MirrorRestClient client, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"contracts/results", [new LimitFilter(100), OrderByFilter.Ascending, .. filters]);
        return client.GetPagedItemsAsync<ContractResultDataPage, ContractResultData>(path);
    }
    /// <summary>
    /// Retrieves the chain ID of the Hedera network that this mirror node is connected to.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The Chain ID of the Hedera network, or zero if not found.
    /// </returns>
    public static async Task<BigInteger> GetChainIdAsync(this MirrorRestClient client)
    {
        var path = GenerateInitialPath($"contracts/results", [new LimitFilter(1)]);
        var data = (await client.GetSingleItemAsync<ContractResultDataPage>(path).ConfigureAwait(false))?.Results?.FirstOrDefault();
        return data?.ChainId ?? throw new MirrorException("Chain ID not found in contract results.", [], System.Net.HttpStatusCode.NotFound);
    }
}