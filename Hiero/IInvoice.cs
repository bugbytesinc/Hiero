namespace Hiero;
/// <summary>
/// Represents a transaction signing request.  
/// This structure is passed to each configured 
/// <see cref="Signatory"/> and signatory callback
/// method to be given the opportunity to sign the
/// request before submitting it to the network.
/// Typically, the signatory will use its private
/// key to sign the <see cref="TransactionBytes"/> serialized
/// representation of the transaction request.  
/// This is the same series of bytes that are sent 
/// to the network along with the signatures 
/// collected from the signatories.
/// </summary>
public interface IInvoice
{
    /// <summary>
    /// The transaction ID assigned to this request. It,
    /// by its nature, contains a timestamp and expiration.
    /// Any callback methods must return from signing this
    /// transaction with enough time for the transaction to
    /// be submitted to the network with sufficient time to
    /// process before becoming invalid.
    /// </summary>
    public TransactionId TransactionId { get; }
    /// <summary>
    /// The memo associated with this transaction, 
    /// provided for convenience.
    /// </summary>
    public string Memo { get; }
    /// <summary>
    /// The bytes created by serializing the request, including
    /// necessary cryptocurrency transfers, into the underlying
    /// network's protobuf format.  This is the exact sequence
    /// of bytes that will be submitted to the network alongside
    /// the signatures created authorizing the request.
    /// </summary>
    public ReadOnlyMemory<byte> TransactionBytes { get; }
    /// <summary>
    /// The smallest desired signature map prefix value length.  
    /// Some network API calls (typically smart contract calls
    /// that interact with other Hedera Services) require the
    /// full public key value to be entered for the prefix. In
    /// this case, this value may request the full length of the 
    /// public keys associated with the signature.  It may also
    /// be (in most cases) zero indicating no particular size
    /// is required.
    /// </summary>
    /// <remarks>
    /// Providing a prefix that is longer or shorter than the 
    /// desired length will not immediately raise an error
    /// unless it results in a prefix mapping conflict (two or
    /// more identical public prefixes producing different signature
    /// values).  However, if too small of a prefix is sent to
    /// the network, the network may reject the transaction under
    /// certain circumstances.  It is recommended to return at 
    /// least the first 6 bytes of the raw public key value when
    /// the prefix size is zero, so that the sdk can orchestrate 
    /// multiple signatures with reasonable probability of not
    /// producing a conflict as described above.
    /// </remarks>
    public int MinimumDesiredPrefixSize { get; }
    /// <summary>
    /// The cancellation token associated with the underlying 
    /// request, may be the default cancellation token, may be
    /// consumed as deemed important by signatory callbacks.
    /// </summary>
    public CancellationToken CancellationToken { get; }
    /// <summary>
    /// Adds a signature to the internal list of signatures
    /// authorizing this request.
    /// </summary>
    /// <param name="type">
    /// The type of signing key used for this signature.
    /// </param>
    /// <param name="publicPrefix">
    /// The first few bytes of the public key associated
    /// with this signature.  This helps the system match
    /// signing requirements held internally in the form of
    /// public keys with the signatures provided.
    /// </param>
    /// <param name="signature">
    /// The bytes representing the signature corresponding
    /// to the associated private/public key.
    /// </param>
    public void AddSignature(KeyType type, ReadOnlySpan<byte> publicPrefix, ReadOnlySpan<byte> signature);
}