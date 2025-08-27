﻿using Proto;

namespace Hiero;

/// <summary>
/// Identifies an account which has been associated 
/// with a given Token or NFT class and therefore
/// hold a balance of that token or NFT.
/// </summary>
public sealed record Association
{
    /// <summary>
    /// The address of the token or NFT that
    /// was associated as a result of the enclosing
    /// transaciton.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The address of the crypto account that
    /// was associated as a result of the enclosing
    /// transaaction.
    /// </summary>
    public EntityId Holder { get; private init; }
    /// <summary>
    /// Internal Helper constructor creating an associaiton
    /// record from raw protobuf.
    /// </summary>
    /// <param name="association"></param>
    internal Association(TokenAssociation association)
    {
        Token = association.TokenId.AsAddress();
        Holder = association.AccountId.AsAddress();
    }
}