// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The data a verifier needs to bootstrap TSS block proof
/// verification: the ledger id (root of trust), the WRAPS
/// verification key, and the current signing roster.
/// </summary>
/// <remarks>
/// This SDK does not verify TSS signatures itself; this record
/// exists so external verification tooling can obtain its inputs
/// through the <see cref="BlockNodeClient"/> server status detail query.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record TssData
{
    /// <summary>
    /// The ledger id — the hash of the genesis address book that
    /// anchors the network's chain of trust.
    /// </summary>
    public ReadOnlyMemory<byte> LedgerId { get; }
    /// <summary>
    /// The WRAPS (recursive SNARK) verification key.
    /// </summary>
    public ReadOnlyMemory<byte> WrapsVerificationKey { get; }
    /// <summary>
    /// The current TSS signing roster.
    /// </summary>
    public IReadOnlyList<TssRosterEntry> Roster { get; }
    /// <summary>
    /// The block number from which this roster is valid.
    /// </summary>
    public ulong ValidFromBlock { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal TssData(Org.Hiero.Block.Api.TssData protobuf)
    {
        LedgerId = protobuf.LedgerId.Memory;
        WrapsVerificationKey = protobuf.WrapsVerificationKey.Memory;
        ValidFromBlock = protobuf.ValidFromBlock;
        if (protobuf.CurrentRoster is not null)
        {
            var entries = new TssRosterEntry[protobuf.CurrentRoster.RosterEntries.Count];
            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = new TssRosterEntry(protobuf.CurrentRoster.RosterEntries[i]);
            }
            Roster = entries;
        }
        else
        {
            Roster = [];
        }
    }
}
