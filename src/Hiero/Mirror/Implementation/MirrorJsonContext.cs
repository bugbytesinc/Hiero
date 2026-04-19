// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;

[JsonSerializable(typeof(MirrorErrorListEnvelope))]
[JsonSerializable(typeof(EvmCallData))]
[JsonSerializable(typeof(EvmCallResult))]
[JsonSerializable(typeof(AccountBalanceDataPage))]
[JsonSerializable(typeof(AccountData))]
[JsonSerializable(typeof(AccountDataPage))]
[JsonSerializable(typeof(BlockData))]
[JsonSerializable(typeof(BlockDataPage))]
[JsonSerializable(typeof(ConsensusNodeDataPage))]
[JsonSerializable(typeof(ContractData))]
[JsonSerializable(typeof(ContractResultData))]
[JsonSerializable(typeof(ContractResultDataPage))]
[JsonSerializable(typeof(ContractStateDataPage))]
[JsonSerializable(typeof(CryptoAllowanceDataPage))]
[JsonSerializable(typeof(ExchangeRateData))]
[JsonSerializable(typeof(ExtendedContractLogDataPage))]
[JsonSerializable(typeof(TopicMessageData))]
[JsonSerializable(typeof(TopicMessageDataPage))]
[JsonSerializable(typeof(TopicData))]
[JsonSerializable(typeof(NetworkFeesData))]
[JsonSerializable(typeof(NftData))]
[JsonSerializable(typeof(TokenAllowanceDataPage))]
[JsonSerializable(typeof(TokenData))]
[JsonSerializable(typeof(TokenHoldingDataPage))]
[JsonSerializable(typeof(TransactionDataPage))]
[JsonSerializable(typeof(TransactionDetailByIdResponse))]
[JsonSerializable(typeof(TransactionDetailDataPage))]
[JsonSerializable(typeof(TransactionTimestampDataPage))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]
internal partial class MirrorJsonContext : JsonSerializerContext
{
}
