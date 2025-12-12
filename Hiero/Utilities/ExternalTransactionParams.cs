using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Sending an Externally Created Transaction 
/// to the network for processing and optionally waiting for a receipt.
/// </summary>
public sealed class ExternalTransactionParams : TransactionParams<TransactionReceipt>
{
    /// <summary>
    /// The serialized protobuf encoded bytes of a <code>SignedTransaction</code>
    /// object to be submitted to a Hedera Gossip Network Node. These bytes must be 
    /// manually created from calling code having a knowledge of how to construct a
    /// proper Hedera network transaction, or by using the 
    /// <code>PrepareExternalTransactionAsync</code> method.
    /// </summary>
    public ReadOnlyMemory<byte> SignedTransactionBytes { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method, particularly useful if setting
    /// the endorsement different from the payer's endorsement.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupts the network transaction submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
}
internal sealed class ExternalTransactionParamsOrchestrator : INetworkParams<TransactionReceipt>
{
    private readonly CancellationToken? _cancellationToken;
    private readonly INetworkTransaction _networkTransaction;
    public Signatory? Signatory => null;
    public CancellationToken? CancellationToken => _cancellationToken;
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction() => _networkTransaction;
    string INetworkParams<TransactionReceipt>.OperationDescription => "External Transaction Submission";
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) => new(transactionId, receipt);
    private ExternalTransactionParamsOrchestrator(CancellationToken? cancellationToken, INetworkTransaction networkTransaction)
    {
        _cancellationToken = cancellationToken;
        _networkTransaction = networkTransaction;
    }
    internal async static Task<(INetworkParams<TransactionReceipt>, INetworkTransaction, ByteString, TransactionID, CancellationToken)> CreateSignedTransactionBytesAsync(ConsensusContextStack context, ExternalTransactionParams externalParams)
    {
        try
        {
            if (externalParams.SignedTransactionBytes.IsEmpty)
            {
                throw new ArgumentOutOfRangeException(nameof(externalParams.SignedTransactionBytes), "Missing Signed Transaction Bytes (was empty).");
            }
            var signedTransaction = SignedTransaction.Parser.ParseFrom(externalParams.SignedTransactionBytes.Span);
            if (signedTransaction.BodyBytes.IsEmpty)
            {
                throw new ArgumentOutOfRangeException(nameof(externalParams.SignedTransactionBytes), "The Signed transaction did not contain a transaction.");
            }
            var transactionBody = TransactionBody.Parser.ParseFrom(signedTransaction.BodyBytes);
            if (!transactionBody.TryGetNetworkTransaction(out INetworkTransaction networkTransaction))
            {
                throw new ArgumentOutOfRangeException(nameof(externalParams.SignedTransactionBytes), "Unrecognized Transaction Type, unable to determine which Hedera Network Service Type should process transaction.");
            }
            var gateway = context.Endpoint;
            if (gateway is null)
            {
                throw new InvalidOperationException("The Network Gateway Node has not been configured. Please check that 'Gateway' is set in the Client context and is compatible with this external transaction.");
            }
            var nodeAddress = transactionBody.NodeAccountID.AsAddress();
            var gatewayAddress = (EntityId)gateway;
            if (nodeAddress != gatewayAddress)
            {
                throw new ArgumentException("The configured Gateway is not compatible with the Node Account ID of this transaction.", nameof(externalParams.SignedTransactionBytes));
            }
            var cancellationToken = externalParams.CancellationToken ?? default;
            var signatories = new Signatory?[] { context.Signatory, externalParams.Signatory }.OfType<ISignatory>().ToArray();
            // Only go to the effort of adding signatures if the signatories
            // exist, if they are null we're just a pass-thru in this context.
            if (signatories.Length > 0)
            {
                // Some of the complexity below is necessary to prevent accidental
                // truncation of signature prefixes and/or duplicate signatures.
                var signaturePrefixTrimLimit = signedTransaction.SigMap is null ?
                    context.SignaturePrefixTrimLimit :
                    Math.Max(context.SignaturePrefixTrimLimit, signedTransaction.SigMap.MaxSignaturePrefixLength);
                var invoice = new Invoice(signedTransaction.BodyBytes.Memory, signaturePrefixTrimLimit, cancellationToken);
                signedTransaction.SigMap?.AddSignaturesToInvoice(invoice);
                foreach (var signatory in signatories)
                {
                    if (signatory.GetSchedule() is not null)
                    {
                        throw new ArgumentException("Scheduling the submission of an external transaction is not supported (one or more signatories in the context were created as pending signatories).  However, the external transaction itself can be a scheduled transaction.", nameof(externalParams.SignedTransactionBytes));
                    }
                    await signatory.SignAsync(invoice).ConfigureAwait(false);
                }
                signedTransaction.SigMap = invoice.GenerateSignedTransactionFromSignatures(true).SigMap;
            }
            var transactionParams = new ExternalTransactionParamsOrchestrator(cancellationToken, networkTransaction);
            return (transactionParams, networkTransaction, signedTransaction.ToByteString(), transactionBody.TransactionID, cancellationToken);
        }
        catch (InvalidProtocolBufferException ipbe)
        {
            throw new ArgumentException("Signed Transaction Bytes not recognized as valid Protobuf.", nameof(externalParams.SignedTransactionBytes), ipbe);
        }
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExternalTransactionParamsExtensions
{
    /// <summary>
    /// Sends an arbitrary externally created Hedera Transaction to the network, 
    /// but does not wait for a receipt, only returning the <code>PRECHECK</code>
    /// code returned from the network (as <code>ResponseCode</code>).
    /// The transaction is submitted as a <code>SignedTransaction</code> object, 
    /// protobuf encoded, and may include signatures in the associated 
    /// <code>sigMap</code> field.  Any Signatories held in the client context 
    /// (or method call) will add signatures to this transaction prior to submitting.  
    /// It is not necessary to include a <code>Payer</code> in the context as the 
    /// transaction itself defines the payer, however a matching (via Node Account ID)
    /// <code>ConsensusNodeEndpoint</code> must be contained in the client's context as it 
    /// provides the necessary gRPC routing to the Hedera Network’s node, which is 
    /// not encoded in the signed transaction structure.
    /// </summary>
    /// <remarks>
    /// Note: this method accepts protobuf encoded as a <code>SignedTransaction</code>,
    /// not a <code>Transaction</code> object as the transaction object contains 
    /// deprecated protobuf fields not supported by this SDK.  The method will perform
    /// the necessary final wrapping of the transaction for final submission.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client submitting the raw transaction to the network.
    /// </param>
    /// <param name="signedTransactionBytes">
    /// The serialized protobuf encoded bytes of a <code>SignedTransaction</code>
    /// object to be submitted to a Hedera Gossip Network Node. These bytes must be 
    /// manually created from calling code having a knowledge of how to construct a
    /// proper Hedera transaction.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The <code>ResponseCode</code> corresponding to the precheck value
    /// returned from the remote Hedera Gossip Node which can indicate
    /// success or failure.  This method does not wait for consensus and does 
    /// not return a receipt.  However, it will retry sending the transaction
    /// when it receives a <code>BUSY</code> response from the remote node.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If transaction submission failed due to network communication errors.  
    /// The PrecheckException will not be thrown for PRECHECK error codes returned from the remote gRPC endpoint.  
    /// This behavior is different from most other API calls that throw a PrecheckException for any precheck value
    /// returned that is not <code>OK</code>.  The PrecheckException is thrown because there is no true response
    /// code to return and the method should divulge some information as to the nature of the network error.</exception>
    public static Task<ResponseCode> SendExternalTransactionAsync(this ConsensusClient client, ReadOnlyMemory<byte> signedTransactionBytes, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return SendExternalTransactionAsync(client, new ExternalTransactionParams { SignedTransactionBytes = signedTransactionBytes }, configure);
    }
    /// <summary>
    /// Sends an arbitrary externally created Hedera Transaction to the network, 
    /// but does not wait for a receipt, only returning the <code>PRECHECK</code>
    /// code returned from the network (as <code>ResponseCode</code>).
    /// The transaction is submitted as a <code>SignedTransaction</code> object, 
    /// protobuf encoded, and may include signatures in the associated 
    /// <code>sigMap</code> field.  Any Signatories held in the client context 
    /// (or method call) will add signatures to this transaction prior to submitting.  
    /// It is not necessary to include a <code>Payer</code> in the context as the 
    /// transaction itself defines the payer, however a matching (via Node Account ID)
    /// <code>ConsensusNodeEndpoint</code> must be contained in the client's context as it 
    /// provides the necessary gRPC routing to the Hedera Network’s node, which is 
    /// not encoded in the signed transaction structure.
    /// </summary>
    /// <remarks>
    /// Note: this method accepts protobuf encoded as a <code>SignedTransaction</code>,
    /// not a <code>Transaction</code> object as the transaction object contains 
    /// deprecated protobuf fields not supported by this SDK.  The method will perform
    /// the necessary final wrapping of the transaction for final submission.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client submitting the raw transaction to the network.
    /// </param>
    /// <param name="signedTransactionBytes">
    /// The serialized protobuf encoded bytes of a <code>SignedTransaction</code>
    /// object to be submitted to a Hedera Gossip Network Node. These bytes must be 
    /// manually created from calling code having a knowledge of how to construct a
    /// proper Hedera transaction.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The <code>ResponseCode</code> corresponding to the precheck value
    /// returned from the remote Hedera Gossip Node which can indicate
    /// success or failure.  This method does not wait for consensus and does 
    /// not return a receipt.  However, it will retry sending the transaction
    /// when it receives a <code>BUSY</code> response from the remote node.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If transaction submission failed due to network communication errors.  
    /// The PrecheckException will not be thrown for PRECHECK error codes returned from the remote gRPC endpoint.  
    /// This behavior is different from most other API calls that throw a PrecheckException for any precheck value
    /// returned that is not <code>OK</code>.  The PrecheckException is thrown because there is no true response
    /// code to return and the method should divulge some information as to the nature of the network error.</exception>
    public static async Task<ResponseCode> SendExternalTransactionAsync(this ConsensusClient client, ExternalTransactionParams externalParams, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var (transactionParams, networkTransaction, signedTransactionBytes, transactionId, cancellationToken) = await ExternalTransactionParamsOrchestrator.CreateSignedTransactionBytesAsync(context, externalParams);
        var transaction = new Transaction { SignedTransactionBytes = signedTransactionBytes };
        var precheck = await Engine.SubmitMessageAsync(context, transaction, networkTransaction.InstantiateNetworkRequestMethod, cancellationToken).ConfigureAwait(false);
        return (ResponseCode)precheck.NodeTransactionPrecheckCode;
    }
    /// <summary>
    /// Submits an arbitrary externally Hedera Transaction to the network.  
    /// The transaction is submitted as a <code>SignedTransaction</code> object, 
    /// protobuf encoded, and may include signatures in the associated 
    /// <code>sigMap</code> field.  Any Signatories held in the client context 
    /// (or method call) will add signatures to this transaction prior to submitting.  
    /// It is not necessary to include a <code>Payer</code> in the context as the 
    /// transaction itself defines the payer, however a mataching (via Node Account ID)
    /// <code>ConsensusNodeEndpoint</code> must be contained in context as it
    /// provides the necessary gRPC routing to the Hedera Network’s node, which is 
    /// not encoded in the signed transaction structure.
    /// </summary>
    /// <remarks>
    /// Note: this method accepts protobuf encoded as a <code>SignedTransaction</code>,
    /// not a <code>Transaction</code> object as the transaction object contains 
    /// depricated protobuf fields not supported by this SDK.  The method will peform
    /// the necessary final wrapping of the transaction for final submission.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client submitting the raw transaction to the network.
    /// </param>
    /// <param name="signedTransactionBytes">
    /// The serialized protobuf encoded bytes of a <code>SignedTransaction</code>
    /// object to be submitted to a Gossip Network TransactionId. These bytes must be 
    /// manually created from calling code having a knowledge of how to construct the 
    /// Hedera transaction.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A receipt for the submitted transaction, if successful, 
    /// otherwise an exception is thrown.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> SubmitExternalTransactionAsync(this ConsensusClient client, ReadOnlyMemory<byte> signedTransactionBytes, Action<IConsensusContext>? configure = null)
    {
        return SubmitExternalTransactionAsync(client, new ExternalTransactionParams { SignedTransactionBytes = signedTransactionBytes }, configure);
    }
    /// <summary>
    /// Submits an arbitrary externally Hedera Transaction to the network.  
    /// The Transaction is submitted as a <code>SignedTransaction</code> object, 
    /// protobuf encoded, and may include signatures in the associated 
    /// <code>sigMap</code> field.  Any Signatories held in the client context 
    /// (or method call) will add signatures to this network transaction prior to submitting.  
    /// It is not necessary to include a <code>Payer</code> in the context as the 
    /// Transaction itself defines the payer, however a matching (via Node Account ID)
    /// <code>ConsensusNodeEndpoint</code> must be contained in context as it
    /// provides the necessary gRPC routing to the Hedera Network’s node, which is 
    /// not encoded in the signed network transaction structure.
    /// </summary>
    /// <remarks>
    /// Note: this method accepts protobuf encoded as a <code>SignedTransaction</code>,
    /// not a <code>Transaction</code> object as the network transaction object contains 
    /// deprecated protobuf fields not supported by this SDK.  The method will perform
    /// the necessary final wrapping of the network transaction for final submission.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client submitting the raw networkTransaction to the network.
    /// </param>
    /// <param name="externalParams">
    /// The serialized protobuf encoded bytes of a <code>SignedTransaction</code>
    /// object to be submitted to a Gossip Network TransactionId. These bytes must be 
    /// manually created from calling code having a knowledge of how to construct the 
    /// Hedera transaction.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A receipt for the submitted network transaction, if successful, 
    /// otherwise an exception is thrown.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the networkTransaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static async Task<TransactionReceipt> SubmitExternalTransactionAsync(this ConsensusClient client, ExternalTransactionParams externalParams, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var (transactionParams, networkTransaction, signedTransactionBytes, transactionId, cancellationToken) = await ExternalTransactionParamsOrchestrator.CreateSignedTransactionBytesAsync(context, externalParams);
        return await Engine.ExecuteAsync(context, signedTransactionBytes, transactionParams, networkTransaction, transactionId, cancellationToken).ConfigureAwait(false);
    }
}
