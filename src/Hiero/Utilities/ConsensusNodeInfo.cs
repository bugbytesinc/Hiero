// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Information regarding a node from the signed
/// address book.
/// </summary>
public sealed record ConsensusNodeInfo
{
    /// <summary>
    /// Identifier of the node (non-sequential)
    /// </summary>
    public long Id { get; internal init; }
    /// <summary>
    /// The RSA public key of the node. Used to sign stream files 
    /// (e.g., record stream files). Precisely, this field is a string 
    /// of hexadecimal characters which, translated to binary, are the 
    /// public key's DER encoding.  
    /// </summary>
    public string RsaPublicKey { get; internal init; } = default!;
    /// <summary>
    /// The crypto account associated with this node.
    /// </summary>
    public EntityId Address { get; internal init; } = default!;
    /// <summary>
    /// Hash of the nodes TLS certificate. This field is a string of 
    /// hexadecimal characters which, translated to binary, are the SHA-384 hash of 
    /// the UTF-8 NFKD encoding of the node's TLS cert in PEM format. Its value can be 
    /// used to verify the node's certificate it presents during TLS negotiations.
    /// </summary>
    public ReadOnlyMemory<byte> CertificateHash { get; internal init; }
    /// <summary>
    /// List of public ip addresses and ports exposed by this node.
    /// </summary>
    public ConsensusNodeEndpointInfo[] Endpoints { get; internal init; } = default!;
    /// <summary>
    /// A Description of the node.
    /// </summary>
    public string Description { get; internal set; } = default!;
}
/// <summary>
/// Extension methods for retrieving consensus node address book information from the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConsensusNodeInfoExtensions
{
    /// <summary>
    /// Retrieves the consensus node address book information from the network.
    /// </summary>
    /// <remarks>
    /// NOTE: this method incurs a charge to retrieve the file from the network.
    /// </remarks>
    /// <param name="client">ConsensusClient Object</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// An array of consensus node information entries from the network's address book.
    /// </returns>
    public static async Task<ConsensusNodeInfo[]> GetAddressBookAsync(this ConsensusClient client, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        // Well known address of the address book file is 0.0.102
        var file = await client.GetFileContentAsync(new EntityId(0, 0, 102), cancellationToken, configure).ConfigureAwait(false);
        var book = NodeAddressBook.Parser.ParseFrom(file.Span);
        return FromAddressBook(book);
    }

    internal static ConsensusNodeInfo[] FromAddressBook(NodeAddressBook book)
    {
        var nodes = book.NodeAddress;
        var count = nodes.Count;
        if (count > 0)
        {
            var result = new ConsensusNodeInfo[count];
            for (var i = 0; i < count; i++)
            {
                var node = nodes[i];
                result[i] = new ConsensusNodeInfo
                {
                    Id = node.NodeId,
                    RsaPublicKey = node.RSAPubKey,
                    Address = node.NodeAccountId.AsAddress(),
                    CertificateHash = node.NodeCertHash.Memory,
                    Endpoints = ToEndpointArray(node.ServiceEndpoint),
                    Description = node.Description
                };
            }
            return result;
        }
        return [];
    }

    private static ConsensusNodeEndpointInfo[] ToEndpointArray(RepeatedField<ServiceEndpoint> endpoints)
    {
        var count = endpoints.Count;
        if (count == 0)
        {
            return [];
        }
        var result = new ConsensusNodeEndpointInfo[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new ConsensusNodeEndpointInfo(endpoints[i]);
        }
        return result;
    }
}
