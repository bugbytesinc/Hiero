using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Diagnostics;

namespace Hiero;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class UtilityExtensions
{
    /// <summary>
    /// Creates a new Transaction ID within the given context.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client having the configuration necessary
    /// to properly generate the Transaction ID.
    /// </param>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IConsensusContext"/> object providing 
    /// the configuration details of this particular method call invocation.
    /// </param>
    /// <returns>
    /// A new Transaction ID that can be used to identify a transaction.
    /// </returns>
    public static TransactionId CreateNewTransactionId(this ConsensusClient client, Action<IConsensusContext>? configure = null)
    {
        var context = client.BuildChildContext(configure);
        try
        {
            return Engine.GetOrCreateTransactionID(context).AsTxId();
        }
        finally
        {
            var vt = context.DisposeAsync();
            if (!vt.IsCompletedSuccessfully)
            {
                _ = completeDispose(vt);
            }
        }

        static async Task completeDispose(ValueTask vt)
        {
            try
            {
                await vt.ConfigureAwait(false);
            }
            catch
            {
                // Ignore any exceptions during dispose, this is just a cleanup.
            }
        }
    }
    /// <summary>
    /// Contacts the configured gateway with a COST_ASK request
    /// to exercise the communications pipeline from this process thru
    /// to the execution engine on the gossip node.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client performing the ping on the configured
    /// gRPC channel to the gossip node.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The time it takes (in miliseconds) to receive a response from
    /// the remote gossip node.
    /// </returns>
    /// <exception cref="PrecheckException">
    /// If the request failed or the gossip node was unreachable.
    /// </exception>
    public static async Task<long> PingAsync(this ConsensusClient client, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var query = new CryptoGetInfoQuery { AccountID = new AccountID(new EntityId(0, 0, 98)) } as INetworkQuery;
        var envelope = query.CreateEnvelope();
        query.SetHeader(new QueryHeader
        {
            Payment = new Transaction { SignedTransactionBytes = ByteString.Empty },
            ResponseType = ResponseType.CostAnswer
        });
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var answer = await Engine.SubmitMessageAsync(context, envelope, query.InstantiateNetworkRequestMethod, shouldRetryRequest, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        var code = answer.ResponseHeader?.NodeTransactionPrecheckCode ?? ResponseCodeEnum.Unknown;
        if (code != ResponseCodeEnum.Ok)
        {
            throw new PrecheckException($"Ping Failed with Code: {code}", TransactionId.None, (ResponseCode)code, 0);
        }
        return stopwatch.ElapsedMilliseconds;

        static bool shouldRetryRequest(Response response)
        {
            return ResponseCodeEnum.Busy == response.ResponseHeader?.NodeTransactionPrecheckCode;
        }
    }
}
