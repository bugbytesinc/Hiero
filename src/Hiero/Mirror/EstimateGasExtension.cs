// SPDX-License-Identifier: Apache-2.0
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
    /// The most mirror calls to make while refining the estimate.  Above one,
    /// the result is always a gas limit the mirror node accepted, and more
    /// calls bring it closer to the smallest such limit; with one, it is the
    /// mirror's raw estimate, which can be too low for calls that earn a
    /// storage refund.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the estimate, including a mirror request
    /// in flight.
    /// </param>
    /// <returns>
    /// An estimate of the gas required to create the contract, including intrinsic gas.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If the create contract params are using the file form of bytecode, this is not supported
    /// by this method as bytes must be passed directly to the mirror node REST API.
    /// </exception>
    public static Task<long> EstimateGasAsync(this MirrorRestClient mirror, EvmAddress from, CreateContractParams createParams, int maxIterations = 10, CancellationToken cancellationToken = default)
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
        return EstimateGasImplementationAsync(mirror, callData, maxIterations, cancellationToken);
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
    /// The most mirror calls to make while refining the estimate.  Above one,
    /// the result is always a gas limit the mirror node accepted, and more
    /// calls bring it closer to the smallest such limit; with one, it is the
    /// mirror's raw estimate, which can be too low for calls that earn a
    /// storage refund.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the estimate, including a mirror request
    /// in flight.
    /// </param>
    /// <returns>
    /// The estimated gas required to call the contract method, including intrinsic gas.
    /// </returns>
    public static Task<long> EstimateGasAsync(this MirrorRestClient mirror, EvmAddress from, CallContractParams callParams, int maxIterations = 10, CancellationToken cancellationToken = default)
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
        return EstimateGasImplementationAsync(mirror, callData, maxIterations, cancellationToken);
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
    /// The most mirror calls to make while refining the estimate.  Above one,
    /// the result is always a gas limit the mirror node accepted, and more
    /// calls bring it closer to the smallest such limit; with one, it is the
    /// mirror's raw estimate, which can be too low for calls that earn a
    /// storage refund.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the estimate, including a mirror request
    /// in flight.
    /// </param>
    /// <returns>
    /// The estimated gas required to call the contract method, including intrinsic gas.
    /// </returns>
    public static Task<long> EstimateGasAsync(this MirrorRestClient mirror, EvmAddress from, QueryContractParams callParams, int maxIterations = 10, CancellationToken cancellationToken = default)
    {
        var callData = new EvmCallData
        {
            From = from,
            To = callParams.Contract.CastToEvmAddress(),
            Gas = Math.Max(callParams.Gas, 15_000_000),
            Data = Abi.EncodeFunctionWithArguments(callParams.MethodName, callParams.MethodArgs),
            EstimateGas = true,
        };
        return EstimateGasImplementationAsync(mirror, callData, maxIterations, cancellationToken);
    }
    /// <summary>
    /// Finds a gas limit the mirror node accepts for the call, by trial
    /// against its simulation.
    /// </summary>
    /// <param name="mirror">
    /// Mirror node to query for the gas estimate.
    /// </param>
    /// <param name="callData">
    /// The call data containing the details of the EVM call, including the contract address, method, and parameters.
    /// </param>
    /// <param name="maxIterations">
    /// The most mirror calls to make while refining the estimate.  Above one,
    /// the result is always a gas limit the mirror node accepted, and more
    /// calls bring it closer to the smallest such limit; with one, it is the
    /// mirror's raw estimate, which can be too low for calls that earn a
    /// storage refund.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the estimate, including a mirror request in flight.
    /// </param>
    /// <returns>
    /// The estimated gas required to complete the EVM call, including intrinsic gas.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The mirror's estimate is a starting point, not necessarily a usable
    /// limit: for a call that earns a storage refund (clearing slots, for
    /// example shortening a stored string) it is the gas used net of the
    /// refund, while execution needs the full amount up front because the
    /// refund is paid back only at the end. So every candidate limit is
    /// tried as the cap of a further estimate, and only one the mirror
    /// accepts is ever returned.
    /// </para>
    /// <para>
    /// If the first estimate is accepted as a cap, estimates made under
    /// tighter caps are followed downward while they keep shrinking and keep
    /// being accepted. If it is not, the limit lies above it: EIP-3529 caps a
    /// refund at a fifth of the gas used, so five quarters of the estimate is
    /// an upper bound (the original cap if even that is refused), and a
    /// binary search narrows the gap. <paramref name="maxIterations"/> bounds
    /// the number of mirror calls; whatever it allows above one, the result
    /// is the lowest limit found to be accepted. With one, it is the mirror's
    /// raw estimate.
    /// </para>
    /// </remarks>
    private static async Task<long> EstimateGasImplementationAsync(MirrorRestClient mirror, EvmCallData callData, int maxIterations, CancellationToken cancellationToken)
    {
        var initialCap = callData.Gas!.Value;
        // A failure at the generous initial cap is a genuine failure of the call.
        var estimate = await EstimateAsync(mirror, callData, cancellationToken).ConfigureAwait(false);
        var calls = 1;
        if (estimate >= initialCap || maxIterations <= 1)
        {
            // With a single call allowed there is nothing to validate against.
            return estimate;
        }
        var accepted = initialCap;
        var candidate = estimate;
        long? rejected = null;
        while (calls < maxIterations)
        {
            callData.Gas = candidate;
            var next = await TryEstimateAsync(mirror, callData, cancellationToken).ConfigureAwait(false);
            calls++;
            if (next is null)
            {
                rejected = candidate;
                break;
            }
            accepted = candidate;
            if (next.Value >= candidate)
            {
                return accepted;
            }
            candidate = next.Value;
        }
        if (rejected is null)
        {
            return accepted;
        }
        if (accepted == initialCap && calls < maxIterations)
        {
            // Nothing below the initial cap has been accepted yet: try the
            // refund bound before falling back to a search up to the cap.
            var refundBound = Math.Min(initialCap, rejected.Value + rejected.Value / 4 + 1);
            callData.Gas = refundBound;
            calls++;
            if (await TryEstimateAsync(mirror, callData, cancellationToken).ConfigureAwait(false) is not null)
            {
                accepted = refundBound;
            }
            else
            {
                rejected = refundBound;
            }
        }
        var low = rejected.Value;
        while (calls < maxIterations && accepted - low > 1)
        {
            var middle = low + (accepted - low) / 2;
            callData.Gas = middle;
            calls++;
            if (await TryEstimateAsync(mirror, callData, cancellationToken).ConfigureAwait(false) is null)
            {
                low = middle;
            }
            else
            {
                accepted = middle;
            }
        }
        return accepted;
    }
    private static async Task<long> EstimateAsync(MirrorRestClient mirror, EvmCallData callData, CancellationToken cancellationToken)
    {
        return (long)new BigInteger((await mirror.CallEvmAsync(callData, cancellationToken).ConfigureAwait(false)).Data.Span, true, true);
    }
    /// <summary>
    /// An estimate under the call's current cap, or null when the mirror
    /// reports <c>INSUFFICIENT_GAS</c> or <c>CONTRACT_REVERT_EXECUTED</c> at
    /// this cap; a revert is treated as a possible out-of-gas failure in a
    /// subcall, whatever its actual cause.
    /// </summary>
    private static async Task<long?> TryEstimateAsync(MirrorRestClient mirror, EvmCallData callData, CancellationToken cancellationToken)
    {
        try
        {
            return await EstimateAsync(mirror, callData, cancellationToken).ConfigureAwait(false);
        }
        catch (MirrorException mce) when (mce.Message == "INSUFFICIENT_GAS" || mce.Message == "CONTRACT_REVERT_EXECUTED")
        {
            return null;
        }
    }
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
    ///// <summary>
    ///// Internal helper function that computes the intrinsic gas for a given call data.
    ///// </summary>
    ///// <param name="callData">
    ///// The call data containing the EVM call details.
    ///// </param>
    ///// <returns>
    ///// The computed intrinsic gas based on the number of zero and non-zero bytes in the data.
    ///// </returns>
    //private static long ComputeIntrinsicGas(EvmCallData callData)
    //{
    //    var zeroBytes = callData.Data!.Value.ToArray().Count(b => b == 0);
    //    var nonZeroBytes = callData.Data.Value.Length - zeroBytes;
    //    return 21000L + 4L * zeroBytes + 16L * nonZeroBytes;
    //}
}
