// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// An inclusive range of block numbers held by a block node.
/// </summary>
/// <param name="Start">The first block number in the range (inclusive).</param>
/// <param name="End">The last block number in the range (inclusive).</param>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockRange(ulong Start, ulong End);
