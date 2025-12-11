namespace Proto;

public sealed partial class Transaction
{
    /// <summary>
    /// Extracts a Transaction ID from the Protobuf Transaction structure
    /// </summary>
    /// <returns>
    /// Protobuf Transaction ID from the Protobuf Transaction structure
    /// </returns>
    internal TransactionID ExtractTransactionID()
    {
        var signedTransaction = SignedTransaction.Parser.ParseFrom(SignedTransactionBytes);
        var transactionBody = TransactionBody.Parser.ParseFrom(signedTransaction.BodyBytes);
        return transactionBody.TransactionID;
    }
}