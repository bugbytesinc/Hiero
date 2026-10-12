// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using System.Diagnostics.CodeAnalysis;

namespace Hiero;

/// <summary>
/// A <see cref="BlockNodeClient"/> instance’s configuration.
/// </summary>
/// <remarks>
/// This interface exposes the current configuration context for a
/// <see cref="BlockNodeClient"/> instance.  When accessed through a
/// <see cref="BlockNodeClient.Configure(Action{IBlockNodeContext})"/>,
/// <see cref="BlockNodeClient.Clone(Action{IBlockNodeContext})"/> or one of the
/// network request methods, calling code can interrogate the
/// object for configuration details and update as necessary.
/// Typically, the bare minimum that must be configured in a
/// context in order to access a block node is the URL of the
/// block node server <see cref="IBlockNodeContext.Uri"/>.
/// The other default values are typically suitable for most
/// interactions with block nodes.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public interface IBlockNodeContext
{
    /// <summary>
    /// gRPC URI identifying a block node providing access to
    /// the Hiero Block Node services (default port 40840).
    /// </summary>
    Uri Uri { get; set; }
    /// <summary>
    /// gRPC URI of the block node's status API (the node service's
    /// server status and server status detail calls), for block nodes
    /// that serve it on a different endpoint than <see cref="Uri"/>.
    /// When unset (or <code>null</code>), status calls go to
    /// <see cref="Uri"/>.
    /// </summary>
    /// <remarks>
    /// Block node operators may publish each Block Node API on its own
    /// port; the HIP-1137 registry advertises a separate endpoint per
    /// API (<code>STATUS</code>, <code>SUBSCRIBE_STREAM</code>,
    /// <code>PUBLISH</code>, …).  Block stream subscription calls the
    /// status API internally to choose and check its starting block,
    /// so subscribing to such a node requires <see cref="Uri"/> to be
    /// its subscribe endpoint and this property to be its status
    /// endpoint.
    /// </remarks>
    Uri? StatusUri { get; set; }
    /// <summary>
    /// The number of times the client software should retry
    /// recoverable communication failures; this includes unary
    /// call retries when the block node is temporarily unavailable
    /// and automatic block stream re-subscription after a stream
    /// gap or transport drop.
    /// </summary>
    int RetryCount { get; set; }
    /// <summary>
    /// The timespan to wait between retry attempts.  Each retry attempt
    /// adds the same additional amount of delay between retries.  The
    /// second attempt will wait twice the delay, the third attempt
    /// will wait three times the delay and so on.
    /// </summary>
    TimeSpan RetryDelay { get; set; }
    /// <summary>
    /// Called by the library just before the protobuf request
    /// is sent to the Block Node.  This is the only exposure
    /// the library provides to the underlying protobuf implementation
    /// (although they are publicly available in the library).
    /// This method can be useful for logging and tracking purposes.
    /// It could also be used in advanced scenarios to modify the
    /// protobuf message just before sending.
    /// </summary>
    Action<IMessage>? OnSendingRequest { get; set; }
    /// <summary>
    /// Clears a property on the context.  This is the only way to clear
    /// a property value on the context so that a parent value can be
    /// used instead.  Setting a value to <code>null</code> does not
    /// clear the value, it sets it to <code>null</code> for the
    /// current context and child contexts.
    /// </summary>
    /// <param name="name">
    /// The name of the property to reset, must be one of the public
    /// properties of the <code>IBlockNodeContext</code>.  We suggest using
    /// the <code>nameof()</code> operator to ensure type safety.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// when an invalid <code>name</code> is provided.
    /// </exception>
    void Reset(string name);
}
