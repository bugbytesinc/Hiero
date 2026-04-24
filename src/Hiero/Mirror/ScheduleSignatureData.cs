// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// A single signature recorded against a scheduled
/// transaction. Appears nested under
/// <see cref="ScheduleData.Signatures"/>.
/// </summary>
public class ScheduleSignatureData
{
    /// <summary>
    /// Consensus timestamp at which this signature was accepted
    /// by the network.
    /// </summary>
    [JsonPropertyName("consensus_timestamp")]
    public ConsensusTimeStamp Consensus { get; set; }
    /// <summary>
    /// Leading bytes of the signer's public key used by the
    /// mirror node to identify the signing key slot.
    /// </summary>
    [JsonPropertyName("public_key_prefix")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> PublicKeyPrefix { get; set; }
    /// <summary>
    /// The signature bytes themselves.
    /// </summary>
    [JsonPropertyName("signature")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> Signature { get; set; }
    /// <summary>
    /// The key-type enum reported by the mirror node — one of
    /// <c>CONTRACT</c>, <c>ED25519</c>, <c>RSA_3072</c>,
    /// <c>ECDSA_384</c>, <c>ECDSA_SECP256K1</c>, or
    /// <c>UNKNOWN</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? KeyType { get; set; }
}
