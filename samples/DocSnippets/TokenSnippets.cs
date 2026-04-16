// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the Token domain (fungible tokens and
// airdrops). See CryptoSnippets.cs for the authoring convention.

using Hiero;

namespace DocSnippets;

public static class TokenSnippets
{
    public static async Task MintToken(
        ConsensusClient client,
        EntityId token)
    {
        #region MintToken
        // Mint 500,000 additional tokens (smallest unit) into the token's
        // treasury account. The transaction must be signed by the token's
        // supply key — supply it via the client's Signatory or on the params.
        var receipt = await client.MintTokenAsync(token, 500_000);
        Console.WriteLine($"Mint status: {receipt.Status}");
        Console.WriteLine($"New circulation: {receipt.Circulation}");
        #endregion
    }

    public static async Task BurnToken(
        ConsensusClient client,
        EntityId token)
    {
        #region BurnToken
        // Burn 250,000 tokens from the treasury account. Like minting, burning
        // requires the supply key signature. The method name is plural:
        // BurnTokensAsync (not BurnTokenAsync).
        var receipt = await client.BurnTokensAsync(token, 250_000);
        Console.WriteLine($"Burn status: {receipt.Status}");
        Console.WriteLine($"New circulation: {receipt.Circulation}");
        #endregion
    }

    public static async Task AssociateToken(
        ConsensusClient client,
        EntityId account,
        EntityId token)
    {
        #region AssociateToken
        // Associate an account with a token so it can hold a balance. This
        // must be signed by the account's key (not the token's admin key).
        // Accounts with auto-association slots skip this step.
        var receipt = await client.AssociateTokenAsync(account, token);
        Console.WriteLine($"Associate status: {receipt.Status}");
        #endregion
    }

    public static async Task AssociateMultipleTokens(
        ConsensusClient client,
        EntityId account,
        EntityId token1,
        EntityId token2,
        EntityId token3)
    {
        #region AssociateMultiple
        // Associate several tokens in a single transaction. One transaction
        // fee, one signature — cheaper than N separate associations.
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = account,
            Tokens = new[] { token1, token2, token3 }
        });
        Console.WriteLine($"Associate status: {receipt.Status}");
        #endregion
    }

    public static async Task DissociateToken(
        ConsensusClient client,
        EntityId token,
        EntityId account)
    {
        #region DissociateToken
        // Remove an account's token-balance storage slot. The account must
        // hold zero balance of the token before dissociating. Argument order
        // is (token, account) — opposite of AssociateTokenAsync.
        var receipt = await client.DissociateTokenAsync(token, account);
        Console.WriteLine($"Dissociate status: {receipt.Status}");
        #endregion
    }

    public static async Task AirdropToken(
        ConsensusClient client,
        EntityId token,
        EntityId sender,
        EntityId recipient,
        long amount)
    {
        #region AirdropToken
        // Airdrop fungible tokens. If the recipient is already associated or
        // has a free auto-association slot, the transfer settles immediately.
        // Otherwise a pending airdrop is created that the recipient claims
        // with ClaimAirdropAsync.
        var receipt = await client.AirdropTokenAsync(token, sender, recipient, amount);
        Console.WriteLine($"Airdrop status: {receipt.Status}");
        #endregion
    }

    public static async Task AirdropNft(
        ConsensusClient client,
        Nft nft,
        EntityId sender,
        EntityId recipient)
    {
        #region AirdropNft
        // Airdrop a single NFT. Same pending-claim semantics as fungible
        // airdrops: if the recipient already holds the collection or has
        // auto-association slots, it settles immediately.
        var receipt = await client.AirdropNftAsync(nft, sender, recipient);
        Console.WriteLine($"NFT airdrop status: {receipt.Status}");
        #endregion
    }

    public static async Task PauseToken(ConsensusClient client, EntityId token)
    {
        #region PauseToken
        // Pause *all* transfers of the token across every holder. Token must
        // have been created with a PauseEndorsement and its key must sign.
        // Reverse with ContinueTokenAsync. Not to be confused with
        // SuspendTokenAsync, which freezes a single holder.
        var receipt = await client.PauseTokenAsync(token);
        Console.WriteLine($"Pause status: {receipt.Status}");
        #endregion
    }

    public static async Task ContinueToken(ConsensusClient client, EntityId token)
    {
        #region ContinueToken
        // Reverse a previous PauseTokenAsync.
        var receipt = await client.ContinueTokenAsync(token);
        Console.WriteLine($"Continue status: {receipt.Status}");
        #endregion
    }

    public static async Task SuspendHolder(
        ConsensusClient client, EntityId token, EntityId holder)
    {
        #region SuspendToken
        // Freeze one holder's ability to move this token. Different from
        // PauseTokenAsync (which affects all holders). Requires the token's
        // SuspendEndorsement key to sign.
        var receipt = await client.SuspendTokenAsync(token, holder);
        Console.WriteLine($"Suspend status: {receipt.Status}");
        #endregion
    }

    public static async Task ResumeHolder(
        ConsensusClient client, EntityId token, EntityId holder)
    {
        #region ResumeToken
        // Reverse a previous SuspendTokenAsync for the given holder.
        var receipt = await client.ResumeTokenAsync(token, holder);
        Console.WriteLine($"Resume status: {receipt.Status}");
        #endregion
    }

    public static async Task GrantKyc(
        ConsensusClient client, EntityId token, EntityId holder)
    {
        #region GrantKyc
        // Mark a holder as KYC-approved for this token. Only tokens created
        // with a GrantKycEndorsement support KYC gating; on those tokens,
        // transfers are rejected until the holder's KYC flag is granted.
        var receipt = await client.GrantTokenKycAsync(token, holder);
        Console.WriteLine($"KYC grant status: {receipt.Status}");
        #endregion
    }

    public static async Task RevokeKyc(
        ConsensusClient client, EntityId token, EntityId holder)
    {
        #region RevokeKyc
        // Clear the KYC-approved flag for a holder. Future transfers by or
        // to this holder will be rejected until KYC is re-granted.
        var receipt = await client.RevokeTokenKycAsync(token, holder);
        Console.WriteLine($"KYC revoke status: {receipt.Status}");
        #endregion
    }

    public static async Task ConfiscateTokens(
        ConsensusClient client, EntityId token, EntityId holder, ulong amount)
    {
        #region ConfiscateTokens
        // Forcibly remove tokens from a holder's balance and send them to
        // nowhere (reduces total circulation). Requires the ConfiscateEndorsement.
        // Method name is plural: ConfiscateTokensAsync.
        var receipt = await client.ConfiscateTokensAsync(token, holder, amount);
        Console.WriteLine($"Confiscate status: {receipt.Status}");
        Console.WriteLine($"Remaining circulation: {receipt.Circulation}");
        #endregion
    }

    public static async Task UpdateTokenMeta(
        ConsensusClient client, EntityId token)
    {
        #region UpdateToken
        // Update mutable token metadata. Only non-null properties are changed;
        // leaving a key null leaves it untouched. Requires Administrator to
        // sign. Immutable tokens (created with no Administrator) cannot be
        // updated by anyone.
        var receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = token,
            Name = "Renamed Token",
            Symbol = "RMT",
            Memo = "Rebranded 2026-Q2"
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task DeleteToken(ConsensusClient client, EntityId token)
    {
        #region DeleteToken
        // Permanently delete a token. Circulation stays in the treasury but
        // no further operations are possible. Requires Administrator to sign.
        // Immutable tokens cannot be deleted.
        var receipt = await client.DeleteTokenAsync(token);
        Console.WriteLine($"Delete status: {receipt.Status}");
        #endregion
    }

    public static async Task UpdateRoyalties(
        ConsensusClient client, EntityId token, EntityId royaltyReceiver)
    {
        #region UpdateRoyalties
        // Replace the royalty (custom transfer fee) schedule on a token.
        // Pass null to clear all royalties. Requires the RoyaltiesEndorsement.
        // Each IRoyalty implementation represents a different fee model —
        // FixedRoyalty (flat fee in a given token), TokenRoyalty (percentage
        // of fungible transfer), or NftRoyalty (royalty on NFT sales).
        var royalties = new IRoyalty[]
        {
            // 2.5% of every fungible transfer goes to royaltyReceiver
            new TokenRoyalty(royaltyReceiver, numerator: 25, denominator: 1000,
                             minimum: 0, maximum: 0)
        };
        var receipt = await client.UpdateRoyaltiesAsync(token, royalties);
        Console.WriteLine($"UpdateRoyalties status: {receipt.Status}");
        #endregion
    }

    public static async Task ClaimAirdrop(
        ConsensusClient client, EntityId sender, EntityId receiver, EntityId token)
    {
        #region ClaimAirdrop
        // Claim a single pending airdrop. The receiver calls this — their
        // payer account must be `receiver`, or their key must be provided
        // via the params overload.
        var pending = new Airdrop(sender, receiver, token);
        var receipt = await client.ClaimAirdropAsync(pending);
        Console.WriteLine($"Claim status: {receipt.Status}");
        #endregion
    }

    public static async Task ClaimMultipleAirdrops(
        ConsensusClient client,
        EntityId sender, EntityId receiver, EntityId token1, Nft nft1)
    {
        #region ClaimAirdropsBatch
        // Claim several pending airdrops in a single transaction. Mixing
        // fungible and NFT pendings in one call is fine.
        var receipt = await client.ClaimAirdropsAsync(new ClaimAirdropParams
        {
            Airdrops = new[]
            {
                new Airdrop(sender, receiver, token1),
                new Airdrop(sender, receiver, nft1)
            }
        });
        Console.WriteLine($"Claim status: {receipt.Status}");
        #endregion
    }

    public static async Task CancelAirdrop(
        ConsensusClient client, EntityId sender, EntityId receiver, EntityId token)
    {
        #region CancelAirdrop
        // Cancel a pending airdrop that hasn't been claimed yet. The sender
        // calls this — cancelling returns the tokens to the sender's balance.
        var pending = new Airdrop(sender, receiver, token);
        var receipt = await client.CancelAirdropAsync(pending);
        Console.WriteLine($"Cancel status: {receipt.Status}");
        #endregion
    }

    public static async Task CancelMultipleAirdrops(
        ConsensusClient client,
        EntityId sender, EntityId receiver1, EntityId receiver2, EntityId token)
    {
        #region CancelAirdropsBatch
        // Cancel several pending airdrops in one transaction.
        var receipt = await client.CancelAirdropsAsync(new CancelAirdropParams
        {
            Airdrops = new[]
            {
                new Airdrop(sender, receiver1, token),
                new Airdrop(sender, receiver2, token)
            }
        });
        Console.WriteLine($"Cancel status: {receipt.Status}");
        #endregion
    }

    public static async Task RelinquishToken(
        ConsensusClient client, EntityId token)
    {
        #region RelinquishToken
        // A holder returns (dissociates) a fungible token they no longer want.
        // Unlike DissociateTokenAsync (which requires zero balance), this
        // actively surrenders any held tokens back to the treasury.
        var receipt = await client.RelinquishTokenAsync(token);
        Console.WriteLine($"Relinquish status: {receipt.Status}");
        #endregion
    }

    public static async Task RelinquishNft(
        ConsensusClient client, Nft nft)
    {
        #region RelinquishNft
        // Surrender a specific NFT back to the treasury. Useful for
        // unwanted airdropped NFTs once they have already been claimed.
        var receipt = await client.RelinquishNftAsync(nft);
        Console.WriteLine($"Relinquish status: {receipt.Status}");
        #endregion
    }

    public static async Task RelinquishMultiple(
        ConsensusClient client,
        EntityId token1, EntityId token2, Nft nft1)
    {
        #region RelinquishBatch
        // Surrender several tokens and NFTs in a single transaction. Any
        // mix of Tokens[] and Nfts[] is allowed.
        var receipt = await client.RelinquishAsync(new RelinquishTokensParams
        {
            Tokens = new[] { token1, token2 },
            Nfts = new[] { nft1 }
        });
        Console.WriteLine($"Relinquish status: {receipt.Status}");
        #endregion
    }

    public static async Task AirdropToMany(
        ConsensusClient client,
        EntityId token,
        EntityId sender,
        EntityId recipient1,
        EntityId recipient2,
        EntityId recipient3)
    {
        #region AirdropMulti
        // Airdrop the same token to three recipients in one transaction. The
        // sender debit (-300) is split into credits totaling +300 across the
        // receivers. Per-token amounts in a TokenTransfers list must sum to
        // zero, just like CryptoTransfer lists.
        var receipt = await client.AirdropAsync(new AirdropParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(token, sender, -300),
                new TokenTransfer(token, recipient1, 100),
                new TokenTransfer(token, recipient2, 100),
                new TokenTransfer(token, recipient3, 100),
            }
        });
        Console.WriteLine($"Airdrop status: {receipt.Status}");
        #endregion
    }
}
