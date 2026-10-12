// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
namespace Hiero;
/// <summary>
/// The Block Node service APIs a registered service endpoint can
/// advertise it serves (HIP-1137).
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum BlockNodeApi
{
    /// <summary>
    /// An API not otherwise enumerated.
    /// </summary>
    Other = 0,
    /// <summary>
    /// The node status API (server status and available ranges).
    /// </summary>
    Status = 1,
    /// <summary>
    /// The block stream publish API (producer side).
    /// </summary>
    Publish = 2,
    /// <summary>
    /// The block stream subscribe API (consumer side).
    /// </summary>
    SubscribeStream = 3,
    /// <summary>
    /// The state proof API.
    /// </summary>
    StateProof = 4,
}
