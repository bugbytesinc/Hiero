using Hiero.Implementation;
using System.Numerics;

namespace Hiero;
/// <summary>
/// Helper class used to orchestrate the creation of an RLP
/// encoded Ethereum transaction to be submitted to the
/// network using the Hedera Ethereum Transaction HAPI endpoint.
/// </summary>
public class EvmTransactionInput
{
    /// <summary>
    /// Conversion from Tinybars to Wei (10^(18-8))
    /// </summary>
    private static readonly BigInteger TinyBarsToWei = BigInteger.Pow(10, 10);
    /// <summary>
    /// The current EVM Nonce associated with the account that
    /// will sign this transaction.
    /// </summary>
    public long EvmNonce { get; set; } = 0;
    /// <summary>
    /// The gas price the signing account is willing to pay
    /// for computational resources to execute the transaction.
    /// </summary>
    public long GasPrice { get; set; } = 0;
    /// <summary>
    /// The maximum amount of gas, in gas units, that the paying 
    /// account is willing to pay to execute the transaction.
    /// </summary>
    public long GasLimit { get; set; } = 0;
    /// <summary>
    /// The recipient of the transaction, which may be a contract 
    /// or another Hedera account.
    /// </summary>
    /// <remarks>
    /// Going forward, this should be the EVM address of the the
    /// account as identified by the mirror node.  It may be a 
    /// long zero EVM address or an ECDSA alias address. To avoid
    /// potential compatibility issues, it is recommended to use
    /// the EVM address for the target account or contract as
    /// identified by the mirror node, not just assume the 
    /// long zero address is the correct address.
    /// </remarks>
    public EvmAddress ToEvmAddress { get; set; } = default!;
    /// <summary>
    /// The amount of HBAR to transfer to the recipient of the transaction.
    /// </summary>
    public long ValueInTinybars { get; set; } = 0;
    /// <summary>
    /// Optimal Method Name to call on the contract, if this is a contract call.
    /// This value must be specified if the transaction is a contract call having
    /// any values in the MethodParameters array.
    /// </summary>
    public string? MethodName { get; set; } = null;
    /// <summary>
    /// Opational method parameters to pass to the contract, if this is a contract call.
    /// </summary>
    public object[]? MethodParameters { get; set; } = null;
    /// <summary>
    /// The corresponding Chain identifier for the network that this transaction
    /// is being submitted to.
    /// </summary>
    public BigInteger ChainId { get; set; } = BigInteger.Zero;
    /// <summary>
    /// Generates an RLP encoded byte array representing the transaction input
    /// values, including the signature if the Signatory is provided.
    /// </summary>
    /// <param name="signatory">
    /// Signatory (Private Key) that will sign the transaction, it must
    /// be a single ECDSA key that corresponds to the EVM address of the account
    /// paying for the transaction.
    /// </param>
    /// <returns>
    /// An array of bytes representing the transaction in EVM RLP encoding.
    /// </returns>
    public ReadOnlyMemory<byte> RlpEncode(Signatory signatory)
    {
        var toAddres = ToEvmAddress.Bytes.ToArray();
        var valueInWei = ValueInTinybars * TinyBarsToWei;
        byte[]? data = null;
        if (!string.IsNullOrEmpty(MethodName))
        {
            data = Abi.EncodeFunctionWithArguments(MethodName, MethodParameters ?? []).ToArray();
        }
        else if (MethodParameters?.Length > 0)
        {
            throw new ArgumentException("Method name must be specified if method parameters are provided.", nameof(MethodName));
        }
        if (signatory is null)
        {
            throw new ArgumentNullException(nameof(signatory), "A Signatory is Required to RLP encode an EVM transaction.");
        }
        var dataToSign = Rlp.Encode(EvmNonce, GasPrice, GasLimit, toAddres, valueInWei, data, ChainId, null, null);
        var (r, s, rid) = ((ISignatory)signatory).SignEvm(dataToSign);
        var v = (ChainId * 2) + 35 + rid;
        return Rlp.Encode(EvmNonce, GasPrice, GasLimit, toAddres, valueInWei, data, v, r, s);
    }
}