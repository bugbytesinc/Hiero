using System.Buffers;
using System.Text.Json;

namespace Hiero.Converters
{
    internal static class ConverterExtensions
    {
        /// <summary>
        /// Search Value table for whitespace.
        /// </summary>
        private static readonly SearchValues<byte> _whitespaceBytes = SearchValues.Create(" \t\n\r"u8);
        /// <summary>
        /// Checks whether a byte utf8text contains only ASCII whitespace.
        /// </summary>
        /// <param name="utf8text">Span to inspect.</param>
        /// <returns>true if the utf8text contains only whitespace, otherwise false.</returns>
        internal static bool IsWhiteSpace(this ReadOnlySpan<byte> utf8text)
        {
            return utf8text.IndexOfAnyExcept(_whitespaceBytes) == -1;
        }

        internal static void WriteHexString(this Utf8JsonWriter writer, ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> data, bool include0x = false)
        {
            writer.WritePropertyName(utf8PropertyName);
            writer.WriteHexStringValue(data, include0x);
        }
        internal static void WriteHexStringValue(this Utf8JsonWriter writer, ReadOnlySpan<byte> data, bool include0x = false)
        {
            var length = data.Length * 2 + (include0x ? 2 : 0);
            byte[]? rented = null;
            Span<byte> buffer = length <= 256
                ? stackalloc byte[length]
                : (rented = ArrayPool<byte>.Shared.Rent(length)).AsSpan(0, length);
            try
            {
                if (include0x)
                {
                    buffer[0] = (byte)'0';
                    buffer[1] = (byte)'x';
                    Convert.TryToHexStringLower(data, buffer.Slice(2), out _);
                }
                else
                {
                    Convert.TryToHexStringLower(data, buffer, out _);
                }
                writer.WriteStringValue(buffer);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }
        public static byte[] ReadHexData(this ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return [];
            }
            if (reader.ValueIsEscaped)
            {
                return DecodeHexOrEmpty(reader.GetString().AsSpan());
            }
            if (reader.HasValueSequence)
            {
                var hexSequence = reader.ValueSequence;
                var byteCount = hexSequence.Length;
                if (byteCount == 0)
                {
                    return [];
                }
                byte[]? rented = null;
                Span<byte> buffer = byteCount <= 64
                    ? stackalloc byte[(int)byteCount]
                    : (rented = ArrayPool<byte>.Shared.Rent((int)byteCount)).AsSpan(0, (int)byteCount);
                try
                {
                    hexSequence.CopyTo(buffer);
                    return DecodeHexOrEmpty(buffer);
                }
                finally
                {
                    if (rented is not null)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
            return DecodeHexOrEmpty(reader.ValueSpan);
        }
        /// <summary>
        /// Decodes an optionally "0x"-prefixed hexadecimal string into bytes, returning an
        /// empty array for null, empty, odd-length, or otherwise malformed input rather than
        /// throwing. Preserves the tolerant "punt to empty" contract of the pre-span Hex decoder.
        /// </summary>
        private static byte[] DecodeHexOrEmpty(ReadOnlySpan<char> hex)
        {
            if (hex.StartsWith("0x") || hex.StartsWith("0X"))
            {
                hex = hex.Slice(2);
            }
            if (hex.Length == 0 || (hex.Length & 1) == 1)
            {
                return [];
            }
            var buffer = new byte[hex.Length / 2];
            return Convert.FromHexString(hex, buffer, out _, out _) == OperationStatus.Done
                ? buffer
                : [];
        }
        /// <summary>
        /// UTF-8 byte-span overload that decodes hexadecimal into bytes, returning an empty
        /// array for empty, odd-length, or otherwise malformed input rather than throwing.
        /// </summary>
        private static byte[] DecodeHexOrEmpty(ReadOnlySpan<byte> utf8Hex)
        {
            if (utf8Hex.StartsWith("0x"u8) || utf8Hex.StartsWith("0X"u8))
            {
                utf8Hex = utf8Hex.Slice(2);
            }
            if (utf8Hex.Length == 0 || (utf8Hex.Length & 1) == 1)
            {
                return [];
            }
            var buffer = new byte[utf8Hex.Length / 2];
            return Convert.FromHexString(utf8Hex, buffer, out _, out _) == OperationStatus.Done
                ? buffer
                : [];
        }
        internal static int CountDigits(long value)
        {
            // value is guaranteed non-negative by constructor
            int digits = 1;
            while (value >= 10) { value /= 10; digits++; }
            return digits;
        }
    }
}