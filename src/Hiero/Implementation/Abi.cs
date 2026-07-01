// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Crypto.Digests;
using System.Buffers.Binary;
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
            AppendFunctionSelectorMapping(buffer, methodArgs[0]);
            for (int i = 1; i < methodArgs.Length; i++)
            {
                buffer.Append(',');
                AppendFunctionSelectorMapping(buffer, methodArgs[i]);
            }
        }
        buffer.Append(')');
        var bytes = GetAsciiBytes(buffer);
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(bytes, 0, bytes.Length);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        return hash.AsMemory(0, 4);
    }
    private static ReadOnlyMemory<byte> EncodeStringPart(object value)
    {
        var text = (string)value;
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var words = (byteCount / 32) + (byteCount % 32 > 0 ? 2 : 1);
        var result = new byte[32 * words];
        WriteInt64(result.AsSpan(0, 32), byteCount);
        Encoding.UTF8.GetBytes(text, result.AsSpan(32));
        return result;
    }
    private static object DecodeStringPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        return Encoding.UTF8.GetString(arg.Slice(32, size).Span);
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
        WriteInt256(bytes.AsSpan(), Convert.ToInt32(value));
        return bytes;
    }
    private static object DecodeInt32Part(ReadOnlyMemory<byte> arg)
    {
        return (int)ReadInt64(arg.Slice(0, 32));
    }
    private static ReadOnlyMemory<byte> EncodeInt64Part(object value)
    {
        var bytes = new byte[32];
        WriteInt256(bytes.AsSpan(), Convert.ToInt64(value));
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
        var bytes = new byte[32];
        WriteUInt256Part(bytes, (BigInteger)value);
        return bytes;
    }
    private static void WriteUInt256Part(Span<byte> destination, BigInteger value)
    {
        if (value.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Negative values cannot be represented as a UInt256.");
        }
        destination.Clear();
        Span<byte> bytes = stackalloc byte[32];
        if (!value.TryWriteBytes(bytes, out var bytesWritten, true, true))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Integer too large to represent as an UInt256");
        }
        bytes.Slice(0, bytesWritten).CopyTo(destination.Slice(32 - bytesWritten));
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
            WriteUInt256Part(result.AsSpan(32 * (i + 1), 32), addresses[i]);
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
            var bytes = new byte[32];
            WriteAddressPart(bytes, address);
            return bytes;
        }
        throw new ArgumentException("Argument was not an address.", nameof(value));
    }
    private static void WriteAddressPart(Span<byte> destination, EntityId address)
    {
        if (address.TryGetEvmAddress(out var evmAddress))
        {
            WriteEvmAddressPart(destination, evmAddress);
        }
        else if (!address.TryGetKeyAlias(out _))
        {
            // For 20 bytes total (aka uint160), packed in 32 bytes, right aligned.
            destination.Clear();
            BinaryPrimitives.WriteInt32BigEndian(destination.Slice(12, 4), (int)address.ShardNum);
            BinaryPrimitives.WriteInt64BigEndian(destination.Slice(16, 8), address.RealmNum);
            BinaryPrimitives.WriteInt64BigEndian(destination.Slice(24, 8), address.AccountNum);
        }
        else
        {
            throw new ArgumentException("Argument was not an address.", nameof(address));
        }
    }
    private static object DecodeAddressPart(ReadOnlyMemory<byte> arg)
    {
        // See EncodeAddressPart for packing notes
        var shard = BinaryPrimitives.ReadInt32BigEndian(arg.Slice(12, 4).Span);
        var realm = BinaryPrimitives.ReadInt64BigEndian(arg.Slice(16, 8).Span);
        var num = BinaryPrimitives.ReadInt64BigEndian(arg.Slice(24, 8).Span);

        return new EntityId(shard, realm, num);
    }

    private static ReadOnlyMemory<byte> EncodeAddressArrayPart(object value)
    {
        var addresses = (EntityId[])value;
        var result = new byte[32 * (addresses.Length + 1)];
        WriteInt64(result.AsSpan(0, 32), addresses.Length);
        for (var i = 0; i < addresses.Length; i++)
        {
            WriteAddressPart(result.AsSpan(32 * (i + 1), 32), addresses[i]);
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

    private static ReadOnlyMemory<byte> EncodeEvmAddressPart(object value)
    {
        if (value is EvmAddress evmAddress)
        {
            var bytes = new byte[32];
            WriteEvmAddressPart(bytes, evmAddress);
            return bytes;
        }
        throw new ArgumentException("Argument was not an EVM address.", nameof(value));
    }
    private static void WriteEvmAddressPart(Span<byte> destination, EvmAddress evmAddress)
    {
        destination.Clear();
        evmAddress.Bytes.CopyTo(destination.Slice(12));
    }
    private static object DecodeEvmAddressPart(ReadOnlyMemory<byte> arg)
    {
        return new EvmAddress(arg.Slice(12, 20).Span);
    }

    private static ReadOnlyMemory<byte> EncodeEvmAddressArrayPart(object value)
    {
        var addresses = (EvmAddress[])value;
        var result = new byte[32 * (addresses.Length + 1)];
        WriteInt64(result.AsSpan(0, 32), addresses.Length);
        for (var i = 0; i < addresses.Length; i++)
        {
            WriteEvmAddressPart(result.AsSpan(32 * (i + 1), 32), addresses[i]);
        }
        return result;
    }
    private static object DecodeEvmAddressArrayPart(ReadOnlyMemory<byte> arg)
    {
        var size = (int)ReadInt64(arg.Slice(0, 32));
        var result = new EvmAddress[size];
        for (var i = 0; i < size; i++)
        {
            result[i] = (EvmAddress)DecodeEvmAddressPart(arg.Slice((i + 1) * 32, 32));
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
        BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(24, 8), value);
    }
    private static void WriteInt256(Span<byte> buffer, long value)
    {
        // Sign-extend negative values across the full 32-byte word so the
        // two's-complement int256 is correct on-chain; the low 8 bytes carry
        // the big-endian value, and non-negative values keep zeroed high bytes.
        buffer.Slice(0, 24).Fill(value < 0 ? (byte)0xFF : (byte)0x00);
        BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(24, 8), value);
    }
    private static long ReadInt64(ReadOnlyMemory<byte> buffer)
    {
        return BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(24, 8).Span);
    }
    private static void WriteUint64(Span<byte> buffer, ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(buffer.Slice(24, 8), value);
    }
    private static ulong ReadUint64(ReadOnlyMemory<byte> buffer)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(24, 8).Span);
    }
    private static byte[] GetAsciiBytes(StringBuilder buffer)
    {
        var byteCount = 0;
        foreach (var chunk in buffer.GetChunks())
        {
            byteCount += Encoding.ASCII.GetByteCount(chunk.Span);
        }

        var bytes = new byte[byteCount];
        var offset = 0;
        foreach (var chunk in buffer.GetChunks())
        {
            offset += Encoding.ASCII.GetBytes(chunk.Span, bytes.AsSpan(offset));
        }
        return bytes;
    }
    private static void AppendFunctionSelectorMapping(StringBuilder buffer, object value)
    {
        if (value is AbiTuple tuple)
        {
            buffer.Append('(');
            if (tuple.Values.Length > 0)
            {
                AppendFunctionSelectorMapping(buffer, tuple.Values[0]);
                for (int i = 1; i < tuple.Values.Length; i++)
                {
                    buffer.Append(',');
                    AppendFunctionSelectorMapping(buffer, tuple.Values[i]);
                }
            }
            buffer.Append(')');
            return;
        }
        buffer.Append(GetMapping(value).AbiCode);
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
            { typeof(EvmAddress), new TypeMapping("address", false, 32, EncodeEvmAddressPart, DecodeEvmAddressPart) },
            { typeof(EvmAddress[]), new TypeMapping("address[]", true, 32, EncodeEvmAddressArrayPart, DecodeEvmAddressArrayPart) },
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
