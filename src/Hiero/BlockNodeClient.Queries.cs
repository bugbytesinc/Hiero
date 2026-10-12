// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;

namespace Hiero;

public sealed partial class BlockNodeClient
{
    /// <summary>
    /// Retrieves the basic status of the block node: the span of
    /// blocks it can currently serve.
    /// </summary>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// The node's available block range and state retention mode.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If communication with the block node failed.</exception>
    public async Task<BlockNodeStatus> GetServerStatusAsync(CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var request = new ServerStatusRequest();
        var response = await ExecuteUnaryAsync(request, static (channel, request, options) => new BlockNodeService.BlockNodeServiceClient(channel).serverStatusAsync(request, options), cancellationToken, configure, statusApi: true).ConfigureAwait(false);
        return new BlockNodeStatus(response);
    }
    /// <summary>
    /// Retrieves the detailed status of the block node: versions,
    /// installed plugins, available and stored block ranges, TSS
    /// verification bootstrap data and consensus address books.
    /// </summary>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// The node's detailed status information.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If communication with the block node failed.</exception>
    public async Task<BlockNodeStatusDetail> GetServerStatusDetailAsync(CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var request = new ServerStatusRequest();
        var response = await ExecuteUnaryAsync(request, static (channel, request, options) => new BlockNodeService.BlockNodeServiceClient(channel).serverStatusDetailAsync(request, options), cancellationToken, configure, statusApi: true).ConfigureAwait(false);
        return new BlockNodeStatusDetail(response);
    }
    /// <summary>
    /// Retrieves a single block by block number.
    /// </summary>
    /// <param name="blockNumber">
    /// The number of the block to retrieve.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// The requested block, or <code>null</code> when the node reports
    /// the block as not found.  A block that falls outside the node's
    /// available range (pruned or not yet produced) raises a
    /// <see cref="BlockNodeException"/> with code
    /// <see cref="BlockNodeExceptionCode.NotAvailable"/> instead.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the block node rejected the request or communication failed.</exception>
    public async Task<Block?> GetBlockAsync(ulong blockNumber, CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var request = new BlockRequest { BlockNumber = blockNumber };
        var response = await ExecuteUnaryAsync(request, static (channel, request, options) => new BlockAccessService.BlockAccessServiceClient(channel).getBlockAsync(request, options), cancellationToken, configure).ConfigureAwait(false);
        return response.Status switch
        {
            BlockResponse.Types.Code.Success => new Block(response.Block),
            BlockResponse.Types.Code.NotFound => null,
            BlockResponse.Types.Code.NotAvailable => throw new BlockNodeException($"Block {blockNumber} is outside the range of blocks available from this block node.", BlockNodeExceptionCode.NotAvailable, null),
            BlockResponse.Types.Code.InvalidRequest => throw new BlockNodeException("The block node rejected the request as invalid.", BlockNodeExceptionCode.InvalidRequest, null),
            _ => throw new BlockNodeException($"The block node reported an unexpected error retrieving block {blockNumber}: {response.Status}", BlockNodeExceptionCode.ServerError, null)
        };
    }
    /// <summary>
    /// Retrieves a single block by block number and checks its
    /// SHA-384 merkle root against the previous-block root hash of the
    /// following block served by the same node, populating
    /// <see cref="Block.VerifiedRootHash"/> on success.
    /// </summary>
    /// <remarks>
    /// Verification recomputes the block's root from its items and
    /// confirms it against the <em>following</em> block's
    /// previous-block root hash — so this method makes a second block
    /// request (the verification anchor) and performs the merkle
    /// recomputation, work the plain <see cref="GetBlockAsync"/> does
    /// not.  The anchor comes from the same node and is not itself
    /// authenticated, so this is an integrity and consistency check,
    /// not authentication: the block proof's signature is not checked.
    /// The block stream's tip cannot be verified this way because
    /// no following block exists yet; there is deliberately no
    /// verified-latest counterpart to <see cref="GetLatestBlockAsync"/>.
    /// </remarks>
    /// <param name="blockNumber">
    /// The number of the block to retrieve and verify.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// The requested, verified block (with
    /// <see cref="Block.VerifiedRootHash"/> populated), or
    /// <code>null</code> when the node reports the block as not found.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">
    /// If verification fails (<see cref="BlockNodeExceptionCode.VerificationFailed"/> —
    /// including when the anchor block is not yet available), the
    /// block holds content this SDK cannot hash with certainty
    /// (<see cref="BlockNodeExceptionCode.Unverifiable"/>), or the
    /// block node rejected the request or communication failed.
    /// </exception>
    public async Task<Block?> GetVerifiedBlockAsync(ulong blockNumber, CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var block = await GetBlockAsync(blockNumber, cancellationToken, configure).ConfigureAwait(false);
        if (block is null)
        {
            return null;
        }
        var result = BlockHasher.ComputeBlockRootHash(block.ToProtobuf());
        if (result.Failure != BlockHashFailure.None)
        {
            var code = result.Failure == BlockHashFailure.Unverifiable ? BlockNodeExceptionCode.Unverifiable : BlockNodeExceptionCode.VerificationFailed;
            throw new BlockNodeException($"Block {blockNumber} cannot be hash verified: {result.Reason}", code, null);
        }
        var computed = result.RootHash!;
        Block? anchor;
        try
        {
            anchor = await GetBlockAsync(blockNumber + 1, cancellationToken, configure).ConfigureAwait(false);
        }
        catch (BlockNodeException ex) when (ex.Code == BlockNodeExceptionCode.NotAvailable)
        {
            anchor = null;
        }
        if (anchor?.Footer is null)
        {
            throw new BlockNodeException($"Block {blockNumber} cannot be verified yet: the following block (the verification anchor) is not available from this node.", BlockNodeExceptionCode.VerificationFailed, null);
        }
        if (!anchor.Footer.PreviousBlockRootHash.Span.SequenceEqual(computed))
        {
            throw new BlockNodeException($"Block {blockNumber} failed hash verification: its recomputed root does not match the following block's previous-block root hash.", BlockNodeExceptionCode.VerificationFailed, null);
        }
        return new Block(block.ToProtobuf(), computed);
    }
    /// <summary>
    /// Retrieves the latest block held by the block node.
    /// </summary>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// The latest block held by the node.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the node holds no blocks, rejected the request, or communication failed.</exception>
    public async Task<Block> GetLatestBlockAsync(CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var request = new BlockRequest { RetrieveLatest = true };
        var response = await ExecuteUnaryAsync(request, static (channel, request, options) => new BlockAccessService.BlockAccessServiceClient(channel).getBlockAsync(request, options), cancellationToken, configure).ConfigureAwait(false);
        return response.Status switch
        {
            BlockResponse.Types.Code.Success => new Block(response.Block),
            BlockResponse.Types.Code.NotFound or BlockResponse.Types.Code.NotAvailable => throw new BlockNodeException("The block node has no blocks available to serve.", BlockNodeExceptionCode.NotAvailable, null),
            BlockResponse.Types.Code.InvalidRequest => throw new BlockNodeException("The block node rejected the request as invalid.", BlockNodeExceptionCode.InvalidRequest, null),
            _ => throw new BlockNodeException($"The block node reported an unexpected error retrieving the latest block: {response.Status}", BlockNodeExceptionCode.ServerError, null)
        };
    }
    /// <summary>
    /// Retrieves a proof that a specific item is contained within a
    /// specific block: the item, the merkle sibling hashes forming the
    /// path to the block root, and the block's aggregate signature.
    /// </summary>
    /// <param name="proofParameters">
    /// Identifies the block and the item of interest; the item may be
    /// specified by complete item, item hash, or item index (exactly one).
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// The proof, or <code>null</code> when the node reports the item
    /// as not found within the block.
    /// </returns>
    /// <exception cref="ArgumentNullException">If required arguments are missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the item specifier is missing or ambiguous.</exception>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the block node rejected the request or communication failed.</exception>
    public async Task<BlockNode.BlockContentsProof?> GetBlockContentsProofAsync(GetBlockContentsProofParams proofParameters, CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        if (proofParameters is null)
        {
            throw new ArgumentNullException(nameof(proofParameters), "Proof Parameters argument is missing. Please check that it is not null.");
        }
        var specifierCount = (proofParameters.Item is not null ? 1 : 0) + (proofParameters.ItemHash.HasValue ? 1 : 0) + (proofParameters.ItemIndex.HasValue ? 1 : 0);
        if (specifierCount != 1)
        {
            throw new ArgumentOutOfRangeException(nameof(proofParameters), "Exactly one of Item, ItemHash or ItemIndex must be specified.");
        }
        var request = new BlockContentsProofRequest { BlockNumber = proofParameters.BlockNumber };
        if (proofParameters.Item is not null)
        {
            request.CompleteBlockItem = proofParameters.Item.ToProtobuf();
        }
        else if (proofParameters.ItemHash.HasValue)
        {
            request.BlockItemHash = ByteString.CopyFrom(proofParameters.ItemHash.Value.Span);
        }
        else
        {
            request.BlockItemIndex = proofParameters.ItemIndex!.Value;
        }
        var response = await ExecuteUnaryAsync(request, static (channel, request, options) => new ProofService.ProofServiceClient(channel).getBlockContentsProofAsync(request, options), cancellationToken, configure).ConfigureAwait(false);
        return response.Status switch
        {
            BlockContentsProofResponse.Types.Code.Success => new BlockNode.BlockContentsProof(response.BlockContentProof),
            BlockContentsProofResponse.Types.Code.ItemNotFound => null,
            BlockContentsProofResponse.Types.Code.DuplicateHashItemFound => throw new BlockNodeException("The item hash matches more than one item in the block; identify the item by index instead.", BlockNodeExceptionCode.InvalidRequest, null),
            BlockContentsProofResponse.Types.Code.NotAvailable => throw new BlockNodeException($"Block {proofParameters.BlockNumber} is outside the range of blocks available from this block node.", BlockNodeExceptionCode.NotAvailable, null),
            BlockContentsProofResponse.Types.Code.InvalidRequest => throw new BlockNodeException("The block node rejected the proof request as invalid.", BlockNodeExceptionCode.InvalidRequest, null),
            _ => throw new BlockNodeException($"The block node reported an unexpected error producing the proof: {response.Status}", BlockNodeExceptionCode.ServerError, null)
        };
    }
    /// <summary>
    /// Retrieves a reference to the network state snapshot as of a
    /// given block number.
    /// </summary>
    /// <param name="lastBlockNumber">
    /// The block number the snapshot must reflect.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// A reference from which the snapshot content can be retrieved.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the block node rejected the request or communication failed.</exception>
    public async Task<StateSnapshot> GetStateSnapshotAsync(ulong lastBlockNumber, CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var request = new StateSnapshotRequest { LastBlockNumber = lastBlockNumber };
        return await GetStateSnapshotAsync(request, cancellationToken, configure).ConfigureAwait(false);
    }
    /// <summary>
    /// Retrieves a reference to the latest network state snapshot the
    /// block node holds.
    /// </summary>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// A reference from which the snapshot content can be retrieved.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the block node rejected the request or communication failed.</exception>
    public async Task<StateSnapshot> GetLatestStateSnapshotAsync(CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var request = new StateSnapshotRequest { RetrieveLatest = true };
        return await GetStateSnapshotAsync(request, cancellationToken, configure).ConfigureAwait(false);
    }
    /// <summary>
    /// Internal implementation of the state snapshot query shared by
    /// the by-number and latest variants.
    /// </summary>
    private async Task<StateSnapshot> GetStateSnapshotAsync(StateSnapshotRequest request, CancellationToken cancellationToken, Action<IBlockNodeContext>? configure)
    {
        var response = await ExecuteUnaryAsync(request, static (channel, request, options) => new StateService.StateServiceClient(channel).stateSnapshotAsync(request, options), cancellationToken, configure).ConfigureAwait(false);
        return response.Status switch
        {
            StateSnapshotResponse.Types.Code.Success => new StateSnapshot(response),
            StateSnapshotResponse.Types.Code.InvalidRequest => throw new BlockNodeException("The block node rejected the state snapshot request as invalid.", BlockNodeExceptionCode.InvalidRequest, null),
            _ => throw new BlockNodeException($"The block node reported an unexpected error retrieving the state snapshot: {response.Status}", BlockNodeExceptionCode.ServerError, null)
        };
    }
    /// <summary>
    /// Shared unary call engine: creates the per-call child context,
    /// invokes the OnSendingRequest hook, sends the request, and
    /// retries transient gRPC failures with linear backoff per the
    /// context's RetryCount/RetryDelay before translating any final
    /// failure into a <see cref="BlockNodeException"/>.  Status API
    /// calls go to the context's StatusUri when one is configured.
    /// </summary>
    private async Task<TResponse> ExecuteUnaryAsync<TRequest, TResponse>(TRequest request, Func<GrpcChannel, TRequest, CallOptions, AsyncUnaryCall<TResponse>> sendRequest, CancellationToken cancellationToken, Action<IBlockNodeContext>? configure, bool statusApi = false) where TRequest : IMessage
    {
        await using var context = CreateChildContext(configure);
        if (context.Uri is null)
        {
            throw new InvalidOperationException("The Block Node URL has not been configured. Please check that 'Uri' is set in the Block Node context.");
        }
        var channel = statusApi ? context.GetStatusChannel() : context.GetChannel();
        var retryCount = Math.Max(0, context.RetryCount);
        var retryDelay = context.RetryDelay;
        context.InstantiateOnSendingRequestHandler()(request);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var options = new CallOptions(cancellationToken: cancellationToken);
                return await sendRequest(channel, request, options).ResponseAsync.ConfigureAwait(false);
            }
            catch (RpcException ex) when ((ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.Unknown) && attempt < retryCount)
            {
                await Task.Delay(retryDelay * (attempt + 1), cancellationToken).ConfigureAwait(false);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The block node request was canceled.", ex, cancellationToken);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                throw new BlockNodeException("The Block Node is not available at this time.", BlockNodeExceptionCode.Unavailable, ex);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.Unimplemented or StatusCode.NotFound)
            {
                // Block nodes serve different subsets of the Block Node
                // APIs; an absent service reports itself at the transport
                // level rather than through a response status.
                throw new BlockNodeException("The Block Node does not implement this API; check which Block Node APIs the endpoint advertises.", BlockNodeExceptionCode.NotImplemented, ex);
            }
            catch (RpcException ex)
            {
                throw new BlockNodeException($"Unable to communicate with the Block Node: {ex.StatusCode}", BlockNodeExceptionCode.CommunicationError, ex);
            }
        }
    }
}
