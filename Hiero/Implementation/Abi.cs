using Org.BouncyCastle.Crypto.Digests;
using System.Numerics;
using System.Text;

namespace Hiero.Implementation;

/// <summary>
/// Internal Helper class for ABI encoding
/// of parameter arguments sent to smart 
/// contract methods.
/// </summary>
internal static class Abi
{
    internal static ReadOnlyMemory<byte> EncodeFunctionWithArguments(string methodName, object[] methodArgs)
    {
        var selector = GetFunctionSelector(methodName, methodArgs);
        var arguments = EncodeArguments(methodArgs);
        var result = new byte[selector.Length + arguments.Length];
        selector.CopyTo(result.AsMemory());
        arguments.CopyTo(result.AsMemory(selector.Length));
        return result;
    }
    internal static ReadOnlyMemory<byte> EncodeArguments(object[] args)
    {
        if (args is null || args.Length == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        var headerSize = 0;
        var totalSize = 0;
        var argsCount = args.Length;
        var parts = new (bool isDynamic, ReadOnlyMemory<byte> bytes)[argsCount];
        for (int i = 0; i < args.Length; i++)
        {
            var (isDynamic, bytes) = parts[i] = EncodePart(args[i]);
            if (isDynamic)
            {
                headerSize += 32;
                totalSize += 32 + bytes.Length;
            }
            else
            {
                headerSize += bytes.Length;
                totalSize += bytes.Length;
            }
        }
        var result = new byte[totalSize];
        var headerPtr = 0;
        var dataPtr = headerSize;
        for (int i = 0; i < argsCount; i++)
        {
            var (isDynamic, bytes) = parts[i];
            if (isDynamic)
            {
                WriteInt64(result.AsSpan(headerPtr), dataPtr);
                bytes.CopyTo(result.AsMemory(dataPtr));
                headerPtr += 32;
                dataPtr += bytes.Length;
            }
            else
            {
                bytes.CopyTo(result.AsMemory(headerPtr));
                headerPtr += bytes.Length;
            }
        }
        return result;
    }
    internal static object[] DecodeArguments(ReadOnlyMemory<byte> data, params Type[] types)
    {
        if (types is null || types.Length == 0)
        {
            return [];
        }
        var results = new object[types.Length];
        var headerPtr = 0;
        for (int i = 0; i < types.Length; i++)
        {
            var typeMapping = GetMapping(types[i]);
            if (typeMapping.IsDynamic)
            {
                var positionPtr = (int)ReadUint64(data.Slice(headerPtr));
                results[i] = typeMapping.Decode(data.Slice(positionPtr));
                headerPtr += typeMapping.HeaderSize;
            }
            else
            {
                results[i] = typeMapping.Decode(data.Slice(headerPtr));
                headerPtr += typeMapping.HeaderSize;
            }
        }
        return results;
    }
    private static (bool isDynamic, ReadOnlyMemory<byte> bytes) EncodePart(object value)
    {
        var mapping = GetMapping(value);
        return (mapping.IsDynamic, mapping.Encode(value));
    }
    private static ReadOnlyMemory<byte> GetFunctionSelector(string methodName, object[] methodArgs)
    {
        var buffer = new StringBuilder(100);
        buffer.Append(methodName);
        buffer.Append('(');
        if (methodArgs != null && methodArgs.Length > 0)
        {
            buffer.Append(GetFunctionSelectorMapping(methodArgs[0]));
            for (int i = 1; i < methodArgs.Length; i++)
            {
                buffer.Append(',');
                buffer.Append(GetFunctionSelectorMapping(methodArgs[i]));
            }
        }
        buffer.Append(')');
        var bytes = Encoding.ASCII.GetBytes(buffer.ToString());
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(bytes, 0, bytes.Length);
        var hash = new byte[digest.GetByteLength()];
        digest.DoFinal(hash, 0);
        return hash.AsMemory(0, 4);
    }
    private static ReadOnlyMemory<byte> EncodeStringPart(object value)
    {
        return EncodeByteArrayPart(Encoding.UTF8.GetBytes(Convert.ToString(value) ?? string.Empty));
    }
    private static object DecodeStringPart(ReadOnlyMemory<byte> arg)
    {
        return Encoding.UTF8.GetString((byte[])DecodeByteArrayPart(arg));
    }
    private static ReadOnlyMemory<byte> EncodeByteArrayPart(object value)
    {
        var bytes = (byte[])value;
        var words = (bytes.Length / 32) + (bytes.Length % 32 > 0 ? 2 : 1);
        var result = new byte[32 * words];
        WriteInt64(result.AsSpan(0, 32), bytes.Length);
        bytes.CopyTo(result.AsSpan(32));
        return result;
    }
    private static object DecodeByteArrayPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        return arg.Slice(32, size).ToArray();
    }
    private static ReadOnlyMemory<byte> EncodeReadOnlyMemoryPart(object value)
    {
        var bytes = (ReadOnlyMemory<byte>)value;
        var words = (bytes.Length / 32) + (bytes.Length % 32 > 0 ? 2 : 1);
        var result = new byte[32 * words];
        WriteInt64(result.AsSpan(0, 32), bytes.Length);
        bytes.CopyTo(result.AsMemory(32));
        return result;
    }
    private static object DecodeReadOnlyMemoryPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        return arg.Slice(32, size);
    }
    private static ReadOnlyMemory<byte> EncodeUInt8Part(object value)
    {
        var bytes = new byte[32];
        WriteInt64(bytes.AsSpan(), Convert.ToByte(value));
        return bytes;
    }
    private static object DecodeUInt8Part(ReadOnlyMemory<byte> arg)
    {
        return (byte)ReadInt64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeUInt16Part(object value)
    {
        var bytes = new byte[32];
        WriteInt64(bytes.AsSpan(), Convert.ToUInt16(value));
        return bytes;
    }
    private static object DecodeUInt16Part(ReadOnlyMemory<byte> arg)
    {
        return (ushort)ReadInt64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeInt32Part(object value)
    {
        var bytes = new byte[32];
        WriteInt64(bytes.AsSpan(), Convert.ToInt32(value));
        return bytes;
    }
    private static object DecodeInt32Part(ReadOnlyMemory<byte> arg)
    {
        return (int)ReadInt64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeInt64Part(object value)
    {
        var bytes = new byte[32];
        WriteInt64(bytes.AsSpan(), Convert.ToInt64(value));
        return bytes;
    }
    private static object DecodeInt64Part(ReadOnlyMemory<byte> arg)
    {
        return ReadInt64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeUInt32Part(object value)
    {
        var bytes = new byte[32];
        WriteUint64(bytes.AsSpan(), Convert.ToUInt32(value));
        return bytes;
    }
    private static object DecodeUInt32Part(ReadOnlyMemory<byte> arg)
    {
        return (uint)ReadUint64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeUInt64Part(object value)
    {
        var bytes = new byte[32];
        WriteUint64(bytes.AsSpan(), Convert.ToUInt64(value));
        return bytes;
    }
    private static object DecodeUInt64Part(ReadOnlyMemory<byte> arg)
    {
        return ReadUint64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeUInt256Part(object value)
    {
        var bytes = ((BigInteger)value).ToByteArray(true, true);
        if (bytes.Length < 32)
        {
            var buff = new byte[32];
            bytes.CopyTo(buff, 32 - bytes.Length);
            return buff;
        }
        if (bytes.Length == 32)
        {
            return bytes;
        }
        throw new ArgumentOutOfRangeException(nameof(value), "Integer too large to represent as an UInt256");
    }
    private static object DecodeUInt256Part(ReadOnlyMemory<byte> arg)
    {
        return new BigInteger(arg.Slice(0, 32).Span, true, true);
    }

    private static ReadOnlyMemory<byte> EncodeUInt256ArrayPart(object value)
    {
        var addresses = (BigInteger[])value;
        var result = new byte[32 * (addresses.Length + 1)];
        WriteInt64(result.AsSpan(0, 32), addresses.Length);
        for (var i = 0; i < addresses.Length; i++)
        {
            EncodeUInt256Part(addresses[i]).CopyTo(result.AsMemory(32 * (i + 1)));
        }
        return result;
    }
    private static object DecodeUInt256ArrayPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        var result = new BigInteger[size];
        for (var i = 0; i < size; i++)
        {
            result[i] = (BigInteger)DecodeUInt256Part(arg.Slice((i + 1) * 32, 32));
        }
        return result;
    }
    private static ReadOnlyMemory<byte> EncodeBoolPart(object value)
    {
        var bytes = new byte[32];
        WriteInt64(bytes.AsSpan(), Convert.ToBoolean(value) ? 1 : 0);
        return bytes;
    }
    private static object DecodeBoolPart(ReadOnlyMemory<byte> arg)
    {
        return ReadInt64(arg.Slice(0, 32)) > 0;
    }
    private static ReadOnlyMemory<byte> EncodeAddressPart(object value)
    {
        if (value is EntityId address)
        {
            if (address.TryGetEvmAddress(out var moniker))
            {
                return EncodeMonikerPart(moniker);
            }
            else if (!address.TryGetKeyAlias(out _))
            {
                // For 20 bytes total (aka uint160)
                // byte 0 to 3 are shard
                // byte 4 to 11 are realm
                // byte 12 to 19 are account number
                // Note: packed in 32 bytes, right aligned

                var bytes = new byte[32];
                var shard = BitConverter.GetBytes(address.ShardNum);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(shard);
                }
                shard[^4..^0].CopyTo(bytes, 12);
                var realm = BitConverter.GetBytes(address.RealmNum);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(realm);
                }
                realm.CopyTo(bytes, 16);
                var num = BitConverter.GetBytes(address.AccountNum);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(num);
                }
                num.CopyTo(bytes, 24);
                return bytes;
            }
        }
        throw new ArgumentException("Argument was not an address.", nameof(value));
    }
    private static object DecodeAddressPart(ReadOnlyMemory<byte> arg)
    {
        // See EncodeAddressPart for packing notes
        var shardAsBytes = arg.Slice(12, 4).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(shardAsBytes);
        }
        var shard = BitConverter.ToInt32(shardAsBytes);

        var realmAsBytes = arg.Slice(16, 8).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(realmAsBytes);
        }
        var realm = BitConverter.ToInt64(realmAsBytes);

        var numAsBytes = arg.Slice(24, 8).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(numAsBytes);
        }
        var num = BitConverter.ToInt64(numAsBytes);

        return new EntityId(shard, realm, num);
    }

    private static ReadOnlyMemory<byte> EncodeAddressArrayPart(object value)
    {
        var addresses = (EntityId[])value;
        var result = new byte[32 * (addresses.Length + 1)];
        WriteInt64(result.AsSpan(0, 32), addresses.Length);
        for (var i = 0; i < addresses.Length; i++)
        {
            EncodeAddressPart(addresses[i]).CopyTo(result.AsMemory(32 * (i + 1)));
        }
        return result;
    }
    private static object DecodeAddressArrayPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        var result = new EntityId[size];
        for (var i = 0; i < size; i++)
        {
            result[i] = (EntityId)DecodeAddressPart(arg.Slice((i + 1) * 32, 32));
        }
        return result;
    }

    private static ReadOnlyMemory<byte> EncodeMonikerPart(object value)
    {
        if (value is EvmAddress moniker)
        {
            var bytes = new byte[32];
            moniker.Bytes.ToArray().CopyTo(bytes, 12);
            return bytes;
        }
        throw new ArgumentException("Argument was not a moniker.", nameof(value));
    }
    private static object DecodeMonikerPart(ReadOnlyMemory<byte> arg)
    {
        var addressAsBigInt = new BigInteger(arg.Slice(0, 32).Span, true, true);
        var minBytes = addressAsBigInt.ToByteArray(true, true);
        var bytes = new byte[20];
        minBytes.CopyTo(bytes, 20 - minBytes.Length);
        return new EvmAddress(bytes.AsSpan());
    }

    private static ReadOnlyMemory<byte> EncodeMonikerArrayPart(object value)
    {
        var addresses = (EvmAddress[])value;
        var result = new byte[32 * (addresses.Length + 1)];
        WriteInt64(result.AsSpan(0, 32), addresses.Length);
        for (var i = 0; i < addresses.Length; i++)
        {
            EncodeMonikerPart(addresses[i]).CopyTo(result.AsMemory(32 * (i + 1)));
        }
        return result;
    }
    private static object DecodeMonikerArrayPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        var result = new EvmAddress[size];
        for (var i = 0; i < size; i++)
        {
            result[i] = (EvmAddress)DecodeMonikerPart(arg.Slice((i + 1) * 32, 32));
        }
        return result;
    }
    private static ReadOnlyMemory<byte> EncodeAbiTuplePart(object value)
    {
        return EncodeArguments(((AbiTuple)value).Values);
    }
    private static object DecodeAbiTuplePart(ReadOnlyMemory<byte> arg)
    {
        // we really need to switch to native .net tuples or ValueTuples
        // then this method would need to be parametrized somehow so it
        // can get to the list of fields to rehydrate.
        // var type = typeof(T);
        // if(type.IsValueType && type.IsGenericType &&type.FullName != null && type.FullName.StartsWith("System.ValueTuple")){
        //   recreate .As<types>
        throw new NotImplementedException();
    }
    private static void WriteInt64(Span<byte> buffer, long value)
    {
        var valueAsBytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(valueAsBytes);
        }
        valueAsBytes.CopyTo(buffer.Slice(24));
    }
    private static long ReadInt64(ReadOnlyMemory<byte> buffer)
    {
        var valueAsBytes = buffer.Slice(24, 8).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(valueAsBytes);
        }
        return BitConverter.ToInt64(valueAsBytes);
    }
    private static void WriteUint64(Span<byte> buffer, ulong value)
    {
        var valueAsBytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(valueAsBytes);
        }
        valueAsBytes.CopyTo(buffer.Slice(24));
    }
    private static ulong ReadUint64(ReadOnlyMemory<byte> buffer)
    {
        var valueAsBytes = buffer.Slice(24, 8).ToArray();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(valueAsBytes);
        }
        return BitConverter.ToUInt64(valueAsBytes);
    }
    private static string GetFunctionSelectorMapping(object value)
    {
        if (value is AbiTuple tuple)
        {
            var buffer = new StringBuilder(100);
            buffer.Append('(');
            if (tuple.Values.Length > 0)
            {
                buffer.Append(GetFunctionSelectorMapping(tuple.Values[0]));
                for (int i = 1; i < tuple.Values.Length; i++)
                {
                    buffer.Append(',');
                    buffer.Append(GetFunctionSelectorMapping(tuple.Values[i]));
                }
            }
            buffer.Append(')');
            return buffer.ToString();
        }
        return GetMapping(value).AbiCode;
    }
    private static TypeMapping GetMapping(object value)
    {
        if (value is null)
        {
            return _typeMap[typeof(int)];
        }
        return GetMapping(value.GetType());
    }
    private static TypeMapping GetMapping(Type type)
    {
        if (_typeMap.TryGetValue(type, out TypeMapping? mapping))
        {
            return mapping;
        }
        throw new InvalidOperationException($"Encoding of type {type.Name} is not currently supported.");
    }
    private static readonly Dictionary<Type, TypeMapping> _typeMap;
    static Abi()
    {
        _typeMap = new Dictionary<Type, TypeMapping>
        {
            { typeof(bool), new TypeMapping("bool", false, 32, EncodeBoolPart, DecodeBoolPart) },
            { typeof(byte), new TypeMapping("uint8", false, 32, EncodeUInt8Part, DecodeUInt8Part) },
            { typeof(ushort), new TypeMapping("uint16", false, 32, EncodeUInt16Part, DecodeUInt16Part) },
            { typeof(int), new TypeMapping("int32", false, 32, EncodeInt32Part, DecodeInt32Part) },
            { typeof(long), new TypeMapping("int64", false, 32, EncodeInt64Part, DecodeInt64Part) },
            { typeof(uint), new TypeMapping("uint32", false, 32, EncodeUInt32Part, DecodeUInt32Part) },
            { typeof(ulong), new TypeMapping("uint64", false, 32, EncodeUInt64Part, DecodeUInt64Part) },
            { typeof(BigInteger), new TypeMapping("uint256", false, 32, EncodeUInt256Part, DecodeUInt256Part) },
            { typeof(BigInteger[]), new TypeMapping("uint256[]", true, 32, EncodeUInt256ArrayPart, DecodeUInt256ArrayPart) },
            { typeof(string), new TypeMapping("string", true, 32, EncodeStringPart, DecodeStringPart) },
            { typeof(byte[]), new TypeMapping("bytes", true, 32, EncodeByteArrayPart, DecodeByteArrayPart) },
            { typeof(ReadOnlyMemory<byte>), new TypeMapping("bytes", true, 32, EncodeReadOnlyMemoryPart, DecodeReadOnlyMemoryPart) },
            { typeof(EntityId), new TypeMapping("address", false, 32, EncodeAddressPart, DecodeAddressPart) },
            { typeof(EntityId[]), new TypeMapping("address[]", true, 32, EncodeAddressArrayPart, DecodeAddressArrayPart) },
            { typeof(EvmAddress), new TypeMapping("address", false, 32, EncodeMonikerPart, DecodeMonikerPart) },
            { typeof(EvmAddress[]), new TypeMapping("address[]", true, 32, EncodeMonikerArrayPart, DecodeMonikerArrayPart) },
            { typeof(AbiTuple), new TypeMapping("()", true, 32, EncodeAbiTuplePart, DecodeAbiTuplePart) },
        };
    }
    internal class TypeMapping
    {
        internal readonly string AbiCode;
        internal readonly bool IsDynamic;
        internal readonly int HeaderSize;
        internal readonly Func<object, ReadOnlyMemory<byte>> Encode;
        internal readonly Func<ReadOnlyMemory<byte>, object> Decode;
        public TypeMapping(string abiCode, bool isDynamic, int headerSize, Func<object, ReadOnlyMemory<byte>> encode, Func<ReadOnlyMemory<byte>, object> decode)
        {
            AbiCode = abiCode;
            IsDynamic = isDynamic;
            HeaderSize = headerSize;
            Encode = encode;
            Decode = decode;
        }
    }
}