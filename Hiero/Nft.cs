using Hiero.Converters;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Hiero;

/// <summary>
/// Represents a unique Hedera Non-Fungible Token (NFT) 
/// identified by its token ID and serial number.
/// </summary>
/// <remarks>
/// This class consists of both an <see cref="EntityId"/> representing 
/// the token of the underlying non fungible token definition and
/// a serial number representing this instance of the nft token.
/// This class is immutable once created.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(NftConverter))]
public sealed record Nft
{
    /// <summary>
    /// The Hedera Non-Fungible Token Type Address
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// Serial number representing the unique instance of the NFT.
    /// </summary>
    public long SerialNumber { get; private init; }
    /// <summary>
    /// A sentinel value representing an uninitialized or non-existent NFT.
    /// It represents the absence of a valid token and serial number.
    /// </summary>
    public static Nft None { get; } = new Nft();
    /// <summary>
    /// Public Constructor, an <code>Nft</code> is immutable after creation.
    /// </summary>
    /// <param name="token">
    /// Main Network Node Payer
    /// </param>
    /// <param name="serialNum">
    /// The serial number of this specific NFT instance (as assigned on mint).
    /// </param>
    public Nft(EntityId token, long serialNum)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token), "Token Id is required.");
        }
        if (token == EntityId.None)
        {
            throw new ArgumentException("Token Id can not be None.", nameof(token));
        }
        if (!token.IsShardRealmNum)
        {
            throw new ArgumentOutOfRangeException(nameof(token), "Token Id must be in the form of [shard.realm.num].");
        }
        if (serialNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serialNum), "Serial Number cannot be negative.");
        }
        Token = token;
        SerialNumber = serialNum;
    }
    /// <summary>
    /// Private constructor for creating a sentinel value representing
    /// </summary>
    private Nft()
    {
        Token = EntityId.None;
        SerialNumber = 0;
    }
    /// <summary>
    /// Attempts to parse a string representation of an NFT
    /// </summary>
    /// <param name="value">String Value to Parse</param>
    /// <param name="nft">Output variable containing a NFT if parsing was successful.</param>
    /// <returns>True if parsing was sucessfull, false if not.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out Nft? nft)
    {
        if (value != null && TryParse(value.AsSpan(), out nft))
        {
            return true;
        }
        nft = null;
        return false;
    }
    /// <summary>
    /// Attempts to parse a string representation of an NFT
    /// </summary>
    /// <param name="value">Sequence of Cahrs (string value) to Parse</param>
    /// <param name="nft">Output variable containing a NFT if parsing was successful.</param>
    /// <returns>True if parsing was sucessfull, false if not.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out Nft? nft)
    {
        nft = null;
        int separator = value.IndexOf('#');
        if (separator <= 0 || separator >= value.Length - 1)
        {
            return false;
        }
        if (!EntityId.TryParseShardRealmNum(value[..separator], out var token) ||
            !uint.TryParse(value[(separator + 1)..], out uint serial))
        {
            return false;
        }
        if (serial == 0)
        {
            if (token == EntityId.None)
            {
                nft = None;
                return true;
            }
            return false;
        }
        nft = new Nft(token, serial);
        return true;
    }
    /// <summary>
    /// Outputs a string representation of the nft in
    /// <code>shard.realm.num#serial</code> form.
    /// </summary>
    /// <returns>
    /// String representation of this nft identifier in its
    /// token#serial format (or "None" for None).
    /// </returns>
    public override string ToString()
    {
        return $"{Token}#{SerialNumber}";
    }
    /// <summary>
    /// Implicit operator for converting an Nft to an EntityId representing
    /// the token portion of this identification.
    /// </summary>
    /// <param name="nft">
    /// The NFT to convert to an EntityId.
    /// </param>
    public static implicit operator EntityId(Nft nft)
    {
        return nft.Token;
    }
}
internal static class NftInstanceExtensions
{
    internal static bool IsNullOrNone([NotNullWhen(false)] this Nft? nft)
    {
        return nft is null || nft == Nft.None;
    }
}