using System.Threading;

namespace Hiero.Implementation;

/// <summary>
/// Defines the common properties required by all query parameter objects.
/// </summary>
internal interface IQueryParams
{
    /// <summary>
    /// An optional cancellation token that can be used to interrupt the transaction
    /// submission process.
    /// </summary>
    CancellationToken? CancellationToken { get; }
    /// <summary>
    /// Constructs the corresponding network query object that encapsulates
    /// the query intent represented by this parameter instance.
    /// </summary>
    /// <returns>
    /// A network query containing instructions to be submitted to the network.
    /// </returns>
    internal INetworkQuery CreateNetworkQuery();
}
