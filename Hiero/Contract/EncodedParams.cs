using Hiero.Converters;
using Hiero.Implementation;
using System.Text.Json.Serialization;

namespace Hiero;
/// <summary>
/// Provides services to decode smart contract ABI data into .net primitives.
/// Typically represents data returned from a smart contract invocation.
/// </summary>
[JsonConverter(typeof(EncodedParamsConverter))]
public class EncodedParams
{
    /// <summary>
    /// The raw bytes returned from a function call (in ABI format).
    /// </summary>
    private readonly byte[] _data;
    /// <summary>
    /// Internal Constructor from the Raw Input
    /// </summary>
    internal EncodedParams(ReadOnlyMemory<byte> data)
    {
        _data = data.ToArray();
    }
    /// <summary>
    /// Constructor from the Raw Input as Hex
    /// </summary>
    internal EncodedParams(string data)
    {
        if (data?.StartsWith("0x") == true)
        {
            data = data[2..];
        }
        if (!string.IsNullOrWhiteSpace(data))
        {
            _data = new byte[data.Length / 2];
            if (!Hex.TryDecode(data.AsSpan(), _data, out _))
            {
                // Not good to get here, it is frustrating
                // that reverts from contract calls return 
                // the ABI, but the native EVM for certain
                // queries will return just an ASCII string,
                // since the predominant use case is considered
                // to be REVERTS from contracts, we'll cast this
                // raw string into an ABI string so downstream
                // code can have a consistent result type.
                _data = Abi.EncodeArguments([data]).ToArray();
            }
        }
        else
        {
            _data = [];
        }
    }
    /// <summary>
    /// The size in bytes of the data (in ABI format) returned from the function call.
    /// </summary>
    public int Size
    {
        get
        {
            return _data.Length;
        }
    }
    /// <summary>
    /// A Readonly copy of the data in raw ABI format.
    /// </summary>
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            return _data;
        }
    }
    /// <summary>
    /// Retrieves the first value returned from the contract cast to the 
    /// desired native type.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the first argument, must be known to the caller.
    /// </typeparam>
    /// <returns>
    /// The value of the first argument decoded from the ABI results.
    /// </returns>
    public T As<T>(int bytesToSkip = 0)
    {
        return (T)Abi.DecodeArguments(bytesToSkip > 0 ? _data[bytesToSkip..] : _data, typeof(T))[0];
    }
    /// <summary>
    /// Retrieves the first and second values from the contract function result cast to the desired types.
    /// </summary>
    /// <typeparam name="T1">
    /// Type of the first argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T2">
    /// Type of the second argument, must be known to the caller.
    /// </typeparam>
    /// <returns>
    /// A tuple of the first two arguments decoded from the contract function ABI results.
    /// </returns>
    public (T1, T2) As<T1, T2>(int bytesToSkip = 0)
    {
        var args = Abi.DecodeArguments(bytesToSkip > 0 ? _data[bytesToSkip..] : _data, typeof(T1), typeof(T2));
        return ((T1)args[0], (T2)args[1]);
    }
    /// <summary>
    /// Retrieves the three values from the contract function result cast to the desired types.
    /// </summary>
    /// <typeparam name="T1">
    /// Type of the first argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T2">
    /// Type of the second argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T3">
    /// Type of the third argument, must be known to the caller.
    /// </typeparam>
    /// <returns>
    /// A tuple of the first three arguments decoded from the contract function ABI results.
    /// </returns>
    public (T1, T2, T3) As<T1, T2, T3>(int bytesToSkip = 0)
    {
        var args = Abi.DecodeArguments(bytesToSkip > 0 ? _data[bytesToSkip..] : _data, typeof(T1), typeof(T2), typeof(T3));
        return ((T1)args[0], (T2)args[1], (T3)args[2]);
    }
    /// <summary>
    /// Retrieves the four values from the contract function result cast to the desired types.
    /// </summary>
    /// <typeparam name="T1">
    /// Type of the first argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T2">
    /// Type of the second argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T3">
    /// Type of the third argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T4">
    /// Type of the fourth argument, must be known to the caller.
    /// </typeparam>
    /// <returns>
    /// A tuple of the first four arguments decoded from the contract function ABI results.
    /// </returns>
    public (T1, T2, T3, T4) As<T1, T2, T3, T4>(int bytesToSkip = 0)
    {
        var args = Abi.DecodeArguments(bytesToSkip > 0 ? _data[bytesToSkip..] : _data, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        return ((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]);
    }
    /// <summary>
    /// Retrieves the five values from the contract function result cast to the desired types.
    /// </summary>
    /// <typeparam name="T1">
    /// Type of the first argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T2">
    /// Type of the second argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T3">
    /// Type of the third argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T4">
    /// Type of the fourth argument, must be known to the caller.
    /// </typeparam>
    /// <typeparam name="T5">
    /// Type of the fifth argument, must be known to the caller.
    /// </typeparam>
    /// <returns>
    /// A tuple of the first five arguments decoded from the contract function ABI results.
    /// </returns>
    public (T1, T2, T3, T4, T5) As<T1, T2, T3, T4, T5>(int bytesToSkip = 0)
    {
        var args = Abi.DecodeArguments(bytesToSkip > 0 ? _data[bytesToSkip..] : _data, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        return ((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]);
    }
    /// <summary>
    /// Retrieves an arbitrary number of values decoded from the contract return data.
    /// </summary>
    /// <param name="types">
    /// An array of native types that should be returned.  Must be known by the caller.
    /// </param>
    /// <returns>
    /// An array of objects (which may be boxed) of the decoded parameters of the types desired.
    /// </returns>
    public object[] GetAll(params Type[] types)
    {
        return Abi.DecodeArguments(_data, types);
    }
}