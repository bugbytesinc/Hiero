using Hiero.Implementation;
using Hiero.Mirror;
using System.ComponentModel;
using System.Numerics;

namespace Hiero.Extensions;
/// <summary>
/// Extends the mirror client functionality to include 
/// estimating the gas required to complete an EVM transaction.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EstimateGasExtension
{
    /// <summary>
    /// Estimates the gas required to create a new contract on 
    /// the Hedera Network using the mirror node REST API.
    /// </summary>
    /// <param name="mirror">
    /// Mirror node to query.
    /// </param>
    /// <param name="from">
    /// The evm address of the account that is creating the contract (payer).
    /// </param>
    /// <param name="createParams">
    /// The create params that would be sent to the HAPI to create the contract.
    /// </param>
    /// <param name="maxIterations">
    /// The number of iterations to attempt to estimate the gas.  Each subsequent try
    /// generally produces a smaller value closer to the limit of what must be paid for the call.
    /// </param>
    /// <returns>
    /// An estimate of the gas required to create the contract, including intrinsic gas.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If the create contract params are using the file form of bytecode, this is not supported
    /// by this method as bytes must be passed directly to the mirror node REST API.
    /// </exception>
    public static Task<long> EstimateGasAsync(this MirrorRestClient mirror, EvmAddress from, CreateContractParams createParams, int maxIterations = 10)
    {
        if (createParams.ByteCode.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(createParams.ByteCode), "Can only estimate gas for contract creates using the ByteCode array property, File property is not supported.");
        }
        var callData = new EvmCallData
        {
            From = from,
            Gas = Math.Max(createParams.Gas, 15_000_000),
            Value = createParams.InitialBalance > 0 ? (ulong)createParams.InitialBalance : null,
            Data = CreateConstructorData(createParams.ByteCode, createParams.ConstructorArgs),
            EstimateGas = true,
        };
        return EstimateGasImplementationAsync(mirror, callData, maxIterations);
    }
    /// <summary>
    /// Estimates the gas required to call a contract method using the mirror node REST API.
    /// </summary>
    /// <param name="mirror">
    /// The mirror node to query for the gas estimate.
    /// </param>
    /// <param name="from">
    /// The EVM address of the account that is calling the contract (payer).
    /// </param>
    /// <param name="callParams">
    /// The HAPI parameters that would be sent to the HAPI to call the contract method.
    /// </param>
    /// <param name="maxIterations">
    /// The number of iterations to attempt to estimate the gas.  Each subsequent try
    /// generally produces a smaller value closer to the limit of what must be paid for the call.
    /// </param>
    /// <returns>
    /// The estimated gas required to call the contract method, including intrinsic gas.
    /// </returns>
    public static Task<long> EstimateGasAsync(this MirrorRestClient mirror, EvmAddress from, CallContractParams callParams, int maxIterations = 10)
    {
        var callData = new EvmCallData
        {
            From = from,
            To = callParams.Contract.CastToEvmAddress(),
            Gas = Math.Max(callParams.Gas, 15_000_000),
            Value = callParams.PayableAmount > 0 ? (ulong)callParams.PayableAmount : null,
            Data = Abi.EncodeFunctionWithArguments(callParams.MethodName, callParams.MethodArgs),
            EstimateGas = true,
        };
        return EstimateGasImplementationAsync(mirror, callData, maxIterations);
    }
    /// <summary>
    /// Estimates the gas required to query a contract method using the mirror node REST API.
    /// </summary>
    /// <param name="mirror">
    /// The mirror node to query for the gas estimate.
    /// </param>
    /// <param name="from">
    /// The EVM address of the account that is calling the contract (payer).
    /// </param>
    /// <param name="callParams">
    /// The HAPI parameters that would be sent to the HAPI to query the contract method.
    /// </param>
    /// <param name="maxIterations">
    /// The number of iterations to attempt to estimate the gas.  Each subsequent try
    /// generally produces a smaller value closer to the limit of what must be paid for the call.
    /// </param>
    /// <returns>
    /// The estimated gas required to call the contract method, including intrinsic gas.
    /// </returns>
    public static Task<long> EstimateGasAsync(this MirrorRestClient mirror, EvmAddress from, QueryContractParams callParams, int maxIterations = 10)
    {
        var callData = new EvmCallData
        {
            From = from,
            To = callParams.Contract.CastToEvmAddress(),
            Gas = Math.Max(callParams.Gas, 15_000_000),
            Data = Abi.EncodeFunctionWithArguments(callParams.MethodName, callParams.MethodArgs),
            EstimateGas = true,
        };
        return EstimateGasImplementationAsync(mirror, callData, maxIterations);
    }
    /// <summary>
    /// Internal method that performs the actual gas estimation by calling the mirror node REST API.
    /// </summary>
    /// <param name="mirror">
    /// Mirror node to query for the gas estimate.
    /// </param>
    /// <param name="callData">
    /// The call data containing the details of the EVM call, including the contract address, method, and parameters.
    /// </param>
    /// <param name="maxIterations">
    /// The number of iterations to attempt to estimate the gas.  Each subsequent try
    /// generally produces a smaller value closer to the limit of what must be paid for the call.
    /// </param>
    /// <returns>
    /// The estimated gas required to complete the EVM call, including intrinsic gas.
    /// </returns>
    private static async Task<long> EstimateGasImplementationAsync(MirrorRestClient mirror, EvmCallData callData, int maxIterations)
    {
        var evmGas = (long)new BigInteger((await mirror.CallEvmAsync(callData)).Data.Span, true, true);
        for (int i = 1; i < maxIterations && evmGas < callData.Gas; i++)
        {
            var previousEvmGas = evmGas;
            try
            {
                callData.Gas = evmGas;
                evmGas = (long)new BigInteger((await mirror.CallEvmAsync(callData)).Data.Span, true, true);
            }
            catch (MirrorException mce) when (i > 3 && (mce.Message == "CONTRACT_REVERT_EXECUTED" || mce.Message == "INSUFFICIENT_GAS"))
            {
                evmGas = previousEvmGas;
                break;
            }
        }
        return evmGas;
    }
    /// <summary>
    /// Helper function that creates the constructor data for a contract creation call.
    /// </summary>
    /// <param name="byteCode">
    /// The byte code of the contract to be created.
    /// </param>
    /// <param name="createArgs">
    /// Additional create arguments to be included in the constructor call.
    /// </param>
    /// <returns>
    /// The constructor data that would be sent to the EVM.
    /// </returns>
    private static ReadOnlyMemory<byte> CreateConstructorData(ReadOnlyMemory<byte> byteCode, object[]? createArgs)
    {
        if (createArgs != null && createArgs.Length > 0)
        {
            var abiArgs = Abi.EncodeArguments(createArgs);
            var data = new byte[byteCode.Length + abiArgs.Length];
            byteCode.Span.CopyTo(data.AsSpan(0, byteCode.Length));
            abiArgs.Span.CopyTo(data.AsSpan(byteCode.Length, abiArgs.Length));
            return data;
        }
        return byteCode;
    }
    // NOTE: Previous versions of mirror nodes appeared to not include
    // intrinsic gas.  This has recently changed, the code below is no
    // longer used, consider removing
    /// <summary>
    /// Internal helper function that computes the intrinsic gas for a given call data.
    /// </summary>
    /// <param name="callData">
    /// The call data containing the EVM call details.
    /// </param>
    /// <returns>
    /// The computed intrinsic gas based on the number of zero and non-zero bytes in the data.
    /// </returns>
    //private static long ComputeIntrinsicGas(EvmCallData callData)
    //{
    //    var zeroBytes = callData.Data!.Value.ToArray().Count(b => b == 0);
    //    var nonZeroBytes = callData.Data.Value.Length - zeroBytes;
    //    return 21000L + 4L * zeroBytes + 16L * nonZeroBytes;
    //}
}
