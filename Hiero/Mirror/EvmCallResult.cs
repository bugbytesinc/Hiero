// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Response from a mirror EVM simulation call.
/// </summary>
public class EvmCallResult
{
    /// <summary>
    /// The ABI data returned from the mirror node simulation
    /// </summary>
    [JsonPropertyName("result")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Result { get; set; } = ReadOnlyMemory<byte>.Empty;
}
/// <summary>
/// Extension methods for simulating EVM contract calls via the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EvmCallResultExtensions
{
    /// <summary>
    /// Simulates a call to the Hedera EVM, can be used for
    /// pure view methods as well.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="callData">
    /// The arguments identifying the contract, method and
    /// arguments to send to the simulated EVM.
    /// </param>
    /// <returns>
    /// The encoded parameters returned from the simulated EVM call.
    /// </returns>
    public static async Task<EncodedParams> CallEvmAsync(this MirrorRestClient client, EvmCallData callData)
    {
        using var response = await client.PostPayload("contracts/call", callData, MirrorJsonContext.Default.EvmCallData);
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync(MirrorJsonContext.Default.EvmCallResult);
            return new EncodedParams(payload?.Result ?? ReadOnlyMemory<byte>.Empty);
        }
        throw await CreateMirrorExceptionAsync(response);
    }
}