// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the Crypto domain.
//
// Each method here corresponds to an <example><code source region="..."/>
// reference in src/Hiero/Crypto/*.cs. Parameters expose everything the
// snippet body needs as free variables, so the excerpted #region reads like
// straight application code.
//
// Never rename a #region without updating the matching <code source="..."
// region="..."/> reference in src/Hiero/Crypto/*.cs. The
// scripts/verify-doc-snippets.sh check catches broken references before
// publish.

using Hiero;

namespace DocSnippets;

public static class CryptoSnippets
{
    public static async Task TransferHbar(
        ConsensusClient client,
        EntityId sender,
        EntityId receiver)
    {
        #region Transfer
        // Transfer 1 HBAR (100,000,000 tinybars) from `sender` to `receiver`.
        // The client's Signatory must satisfy the sender's signing requirement.
        var receipt = await client.TransferAsync(sender, receiver, 100_000_000);
        Console.WriteLine($"Transfer status: {receipt.Status}");
        #endregion
    }

    public static async Task TransferHbarMultiparty(
        ConsensusClient client,
        EntityId sender1,
        EntityId sender2,
        EntityId receiver)
    {
        #region TransferMultiparty
        // Atomic multi-party transfer: two senders each contribute 0.5 HBAR,
        // one receiver takes the full 1 HBAR. Every negative amount must be
        // authorized by the sending account's signatory, and the sum across
        // the transfer list must be zero.
        var receipt = await client.TransferAsync(new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(sender1, -50_000_000),
                new CryptoTransfer(sender2, -50_000_000),
                new CryptoTransfer(receiver, 100_000_000),
            }
        });
        Console.WriteLine($"Transfer status: {receipt.Status}");
        #endregion
    }

    public static async Task TransferToken(
        ConsensusClient client,
        EntityId token,
        EntityId sender,
        EntityId receiver,
        long units)
    {
        #region TransferToken
        // Transfer fungible token units between two accounts. The receiver
        // must already be associated with the token (AssociateTokenAsync) or
        // have automatic-association slots available.
        var receipt = await client.TransferTokenAsync(token, sender, receiver, units);
        Console.WriteLine($"Token transfer status: {receipt.Status}");
        #endregion
    }

    public static async Task TransferNft(
        ConsensusClient client,
        Nft nft,
        EntityId sender,
        EntityId receiver)
    {
        #region TransferNft
        // Transfer a single NFT by (token, serial). Use the atomic multi-asset
        // TransferAsync(TransferParams) overload if you need approvals or want
        // to move several NFTs in one transaction.
        var receipt = await client.TransferNftAsync(nft, sender, receiver);
        Console.WriteLine($"NFT transfer status: {receipt.Status}");
        #endregion
    }

    public static async Task CreateAccount(
        ConsensusClient client,
        Endorsement newAccountEndorsement)
    {
        #region CreateAccount
        // Create a new account funded with 1 HBAR from the client's Payer.
        // `newAccountEndorsement` is the public key or key list that will
        // authorize future transactions on the new account.
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            Endorsement = newAccountEndorsement,
            InitialBalance = 100_000_000, // 1 HBAR in tinybars
            Memo = "Created via Hiero SDK"
        });
        Console.WriteLine($"New account: {receipt.Address}");
        #endregion
    }

    public static async Task UpdateAccount(
        ConsensusClient client,
        EntityId account,
        Endorsement newEndorsement,
        Signatory newSignatory)
    {
        #region UpdateAccount
        // Rotate the signing key on an existing account. The network requires
        // both the *current* key and the *new* key to sign — supply the new
        // signatory on the params so it is combined with the existing context
        // signatory during signing.
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = account,
            Endorsement = newEndorsement,
            Memo = "Key rotation 2026-Q2",
            Signatory = newSignatory
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task DeleteAccount(
        ConsensusClient client,
        EntityId accountToDelete,
        EntityId fundsReceiver,
        Signatory accountSignatory)
    {
        #region DeleteAccount
        // Delete an account and sweep its remaining HBAR to `fundsReceiver`.
        // The account being deleted must sign the transaction; supply its
        // key via Signatory if it is not already in the context.
        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = accountToDelete,
            FundsReceiver = fundsReceiver,
            Signatory = accountSignatory
        });
        Console.WriteLine($"Delete status: {receipt.Status}");
        #endregion
    }

    public static async Task AllocateAllowance(
        ConsensusClient client,
        EntityId owner,
        EntityId spender)
    {
        #region AllocateAllowance
        // Grant `spender` the right to spend up to 5 HBAR from `owner`.
        // Owner must be the client's Payer — allowances always originate
        // from the transaction payer. Setting Amount = 0 revokes the grant.
        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[]
            {
                new CryptoAllowance(owner, spender, 500_000_000) // 5 HBAR
            }
        });
        Console.WriteLine($"Allowance status: {receipt.Status}");
        #endregion
    }
}
