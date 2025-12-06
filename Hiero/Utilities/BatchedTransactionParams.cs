using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Submits a batchParams of transactions to the network for processing.
/// </summary>
public sealed class BatchedTransactionParams
{
    public IReadOnlyList<TransactionParams> TransactionParams { get; set; } = default!;
    /// <summary>
    /// Optional explicit endorsement to be applied to the batchParams of transactions if not specified
    /// individually by each <see cref="BatchedTransaction"/>. If not specified by either the batchParams 
    /// transaction entry or this property, the default endorsement will be used, which match the 
    /// signatories of the client's context.
    /// </summary>
    public Endorsement? Endorsement { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method, particularly useful if setting
    /// the endorsement different from the payer's endorsement.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the transaction submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
}
internal sealed class BatchedParamsOrchestrator : INetworkParams
{
    private readonly Signatory? _signatory;
    private readonly CancellationToken? _cancellationToken;
    private readonly INetworkTransaction _networkTransaction;

    public Signatory? Signatory => _signatory;
    public CancellationToken? CancellationToken => _cancellationToken;
    INetworkTransaction INetworkParams.CreateNetworkTransaction() => _networkTransaction;

    private BatchedParamsOrchestrator(Signatory? signatory, CancellationToken? cancellationToken, INetworkTransaction networkTransaction)
    {
        _signatory = signatory;
        _cancellationToken = cancellationToken;
        _networkTransaction = networkTransaction;
    }

    public static async Task<INetworkParams> CreateAsync(BatchedTransactionParams batchParams, ConsensusClient client)
    {
        var count = batchParams.TransactionParams?.Count ?? 0;
        if (count == 0)
        {
            throw new ArgumentException("The Transactions list must contain at least one batchable transaction.", nameof(batchParams.TransactionParams));
        }
        await using var context = client.CreateChildContext(null);
        var defaultPayer = context.Payer ?? throw new InvalidOperationException("No Payer Account configured for this transaction.");
        var explicitTransactionId = context.TransactionId;
        Key? defaultBatchKey = null;
        var transactionBodies = new TransactionBody[count];
        var signatories = new Signatory[count];
        var signTasks = new Task<ByteString>[count];
        var cancellationTokens = new List<CancellationToken>(count + 1);
        for (int i = 0; i < count; i++)
        {
            var transactionParams = batchParams.TransactionParams![i] ?? throw new ArgumentNullException(nameof(batchParams.TransactionParams), $"The batch transaction at index {i} is null.");
            var batchMetadata = transactionParams as BatchedTransactionMetadata;
            var networkParams = (batchMetadata?.TransactionParams ?? transactionParams) as INetworkParams;
            if (networkParams is null)
            {
                // TODO: This still leaves too many edge cases dangling
                // all of this needs to be rethought and cleaned up.
                var externalParamms = ((batchMetadata?.TransactionParams ?? transactionParams) as ExternalTransactionParams) ?? throw new ArgumentException($"The transaction at index {i} is not a batchable transaction.", nameof(batchParams.TransactionParams));
                if (externalParamms.CancellationToken?.CanBeCanceled == true)
                {
                    cancellationTokens.Add(externalParamms.CancellationToken.Value);
                }
                if (externalParamms.Signatory is not null)
                {
                    throw new NotSupportedException($"The transaction at index {i} is an externally created transaction paired with an additional signer, this is not supported at this time.");
                }
                signTasks[i] = Task.FromResult(ByteString.CopyFrom(externalParamms.SignedTransactionBytes.Span));
            }
            else
            {
                var networkTransaction = networkParams.CreateNetworkTransaction();
                var transactionBody = networkTransaction.CreateTransactionBody();
                transactionBody.BatchKey = batchMetadata?.Endorsement is null ? getDefaultBatchKey() : new Key(batchMetadata.Endorsement);
                transactionBody.TransactionID = computeTransactionId(batchMetadata, i);
                transactionBody.TransactionFee = (ulong)context.FeeLimit;
                transactionBody.TransactionValidDuration = new Duration(context.TransactionDuration);
                transactionBody.Memo = batchMetadata?.Memo ?? "";
                transactionBody.NodeAccountID = new AccountID { AccountNum = 0 };
                transactionBodies[i] = transactionBody;
                if (networkParams.CancellationToken?.CanBeCanceled == true)
                {
                    cancellationTokens.Add(networkParams.CancellationToken.Value);
                }
                signatories[i] = coalesceSignatories(batchMetadata, networkParams, i);
            }
        }
        if (batchParams.CancellationToken?.CanBeCanceled == true)
        {
            cancellationTokens.Add(batchParams.CancellationToken.Value);
        }
        var cancellationToken = cancellationTokens.Count switch
        {
            0 => default,
            1 => cancellationTokens[0],
            _ => CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens.ToArray()).Token
        };
        for (int i = 0; i < count; i++)
        {
            if (signTasks[i] is null)
            {
                signTasks[i] = signBatchedTransactionAsync(transactionBodies[i], signatories[i]);
            }
        }
        var signedTransactions = await Task.WhenAll(signTasks).ConfigureAwait(false);
        var atomicBatchTransactionBody = new AtomicBatchTransactionBody();
        atomicBatchTransactionBody.Transactions.AddRange(signedTransactions);
        return new BatchedParamsOrchestrator(batchParams.Signatory, cancellationToken, atomicBatchTransactionBody);

        Key getDefaultBatchKey()
        {
            if (defaultBatchKey is null)
            {
                if (batchParams.Endorsement is not null)
                {
                    defaultBatchKey = new Key(batchParams.Endorsement);
                }
                else
                {
                    var batchKeySignatory = context.Signatory ?? batchParams.Signatory ?? throw new InvalidOperationException("No Signatories found for this batched transaction.");
                    var batchEndorsements = batchKeySignatory.GetEndorsements();
                    if (batchEndorsements.Count == 0)
                    {
                        throw new InvalidOperationException("Unable to derive the batch key endorsment from the primary signatory for this batched transaction.");
                    }
                    if (batchEndorsements.Count == 1)
                    {
                        defaultBatchKey = new Key(batchEndorsements[0]);
                    }
                    else
                    {
                        defaultBatchKey = new Key
                        {
                            KeyList = new KeyList([.. batchEndorsements])
                        };
                    }
                }
            }
            return defaultBatchKey;
        }

        TransactionID computeTransactionId(BatchedTransactionMetadata? batchedMetadata, int index)
        {
            if (explicitTransactionId is null)
            {
                // We create these transaction IDs temporably before the outter transaction is
                // created, so they fall within the same valid start time more or less.
                var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(context.AdjustForLocalClockDrift);
                return new TransactionID
                {
                    AccountID = new AccountID(batchedMetadata?.Payer ?? defaultPayer),
                    TransactionValidStart = new Timestamp
                    {
                        Seconds = seconds,
                        Nanos = nanos
                    }
                };
            }
            else
            {
                // This Ensures the transaction start times of interior batched
                // transactions fall within the same valid time frame as the outer
                // transaction if the outter transaction start time is explicitly set.
                var seconds = explicitTransactionId.ValidStartSeconds;
                var nanos = explicitTransactionId.ValidStartNanos - count + index;
                if (nanos < 0)
                {
                    seconds--;
                    nanos += 1_000_000_000;
                }
                return new TransactionID
                {
                    AccountID = new AccountID(batchedMetadata?.Payer ?? explicitTransactionId.Payer),
                    TransactionValidStart = new Timestamp
                    {
                        Seconds = seconds,
                        Nanos = nanos
                    }
                };
            }
        }

        Signatory coalesceSignatories(BatchedTransactionMetadata? batchMetadata, INetworkParams transactionParams, int i)
        {
            var signatoryList = new List<Signatory>(3);
            if (transactionParams.Signatory is not null)
            {
                signatoryList.Add(transactionParams.Signatory);
            }
            if (batchMetadata?.Payer is null)
            {
                if (context.Signatory is not null)
                {
                    signatoryList.Add(context.Signatory);
                }
                if (signatoryList.Count == 0 && batchParams.Signatory is not null)
                {
                    signatoryList.Add(batchParams.Signatory);
                }
            }
            else
            {
                if (signatoryList.Count == 0 && batchParams.Signatory is not null)
                {
                    signatoryList.Add(batchParams.Signatory);
                }
                if (signatoryList.Count == 0 && context.Signatory is not null)
                {
                    signatoryList.Add(context.Signatory);
                }
            }
            if (signatoryList.Count == 0)
            {
                throw new InvalidOperationException($"Unable to find a Signatory for batch transaction at index {i}.");
            }
            return new Signatory(signatoryList.ToArray());
        }

        async Task<ByteString> signBatchedTransactionAsync(TransactionBody transactionBody, ISignatory signatory)
        {
            var invoice = new Invoice(transactionBody, context.SignaturePrefixTrimLimit, cancellationToken);
            await signatory.SignAsync(invoice).ConfigureAwait(false);
            return invoice.GenerateSignedTransactionFromSignatures(true).ToByteString();
        }
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Atomic Batch Transaction";
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionBatchParamsExtensions
{
    /// <summary>
    /// Generates a psudo random number, which can be retrieved via the
    /// transaction's record.
    /// </summary>
    /// <param name="maxValue">The maximum allowed value for
    /// the generated number.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating the success of the operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static async Task<TransactionReceipt> SubmitTransactionBatchAsync(this ConsensusClient client, BatchedTransactionParams batchParams, Action<IConsensusContext>? configure = null)
    {
        await using var configuredClient = client.Clone(configure);
        var transactionParams = await BatchedParamsOrchestrator.CreateAsync(batchParams, configuredClient).ConfigureAwait(false);
        return await configuredClient.ExecuteNetworkParamsAsync<TransactionReceipt>(transactionParams, null).ConfigureAwait(false);
    }
}
