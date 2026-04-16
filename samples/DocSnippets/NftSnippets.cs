// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the NFT domain. See CryptoSnippets.cs for
// the authoring convention. The NFT "create" and "mint-batch" scenarios are
// demonstrated by samples/CreateNft/Program.cs (regions CreateNft and
// MintNftBatch) — this file covers single-metadata mint and burn.

using System.Text;
using Hiero;

namespace DocSnippets;

public static class NftSnippets
{
    public static async Task MintSingleNft(
        ConsensusClient client,
        EntityId nftCollection)
    {
        #region MintNftSingle
        // Mint a single NFT with UTF-8-encoded JSON metadata (CID-style strings
        // are the typical choice — the network does not interpret the bytes).
        // The supply key must sign. The receipt's SerialNumbers array holds
        // the newly issued serial number.
        var metadata = Encoding.UTF8.GetBytes("{\"name\":\"Artifact #1\"}");
        var receipt = await client.MintNftAsync(nftCollection, metadata);
        Console.WriteLine($"Minted serial: {receipt.SerialNumbers[0]}");
        #endregion
    }

    public static async Task BurnSingleNft(
        ConsensusClient client,
        EntityId nftCollection,
        long serial)
    {
        #region BurnNftSingle
        // Burn one specific NFT by (token, serial). The NFT must be held by
        // the treasury — NFTs in arbitrary wallets must be transferred back to
        // the treasury first, or confiscated via ConfiscateNftAsync.
        var receipt = await client.BurnNftAsync(new Nft(nftCollection, serial));
        Console.WriteLine($"Burn status: {receipt.Status}");
        Console.WriteLine($"Remaining circulation: {receipt.Circulation}");
        #endregion
    }

    public static async Task ConfiscateNft(
        ConsensusClient client, Nft nft, EntityId account)
    {
        #region ConfiscateNftSingle
        // Forcibly remove one NFT from an arbitrary holder's wallet and
        // return it to the token's treasury. Requires the NFT collection's
        // ConfiscateEndorsement (wipe key). Method name is singular for the
        // single-NFT overload.
        var receipt = await client.ConfiscateNftAsync(nft, account);
        Console.WriteLine($"Confiscate status: {receipt.Status}");
        #endregion
    }

    public static async Task ConfiscateMultipleNfts(
        ConsensusClient client, EntityId collection, EntityId account)
    {
        #region ConfiscateNftBatch
        // Confiscate several NFTs from the same collection and holder in one
        // transaction. Method name becomes plural (ConfiscateNftsAsync) for
        // the params-object form.
        var receipt = await client.ConfiscateNftsAsync(new ConfiscateNftParams
        {
            Token = collection,
            Account = account,
            SerialNumbers = new long[] { 1, 2, 3 }
        });
        Console.WriteLine($"Confiscate status: {receipt.Status}");
        #endregion
    }

    public static async Task UpdateNftMetadata(
        ConsensusClient client, Nft nft)
    {
        #region UpdateNftMetadataSingle
        // Replace the metadata bytes on one specific NFT. Requires the
        // collection's MetadataEndorsement to sign. Common for rotating
        // off-chain JSON references after the initial mint.
        var newMetadata = Encoding.UTF8.GetBytes("{\"name\":\"Revised\"}");
        var receipt = await client.UpdateNftMetadataAsync(nft, newMetadata);
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task UpdateMultipleNftMetadata(
        ConsensusClient client, EntityId collection)
    {
        #region UpdateNftMetadataBatch
        // Apply the *same* metadata blob to multiple serials at once. Use
        // the params form when rotating a shared metadata reference across a
        // set of NFTs in one collection. Method names: singular-one is
        // `UpdateNftMetadataAsync`, plural-batch is `UpdateNftsMetadataAsync`.
        var receipt = await client.UpdateNftsMetadataAsync(new UpdateNftsParams
        {
            Token = collection,
            SerialNumbers = new long[] { 1, 2, 3 },
            Metadata = Encoding.UTF8.GetBytes("{\"cid\":\"bafy...\"}")
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task BurnMultipleNfts(
        ConsensusClient client,
        EntityId nftCollection)
    {
        #region BurnNftBatch
        // Burn several NFTs from the same collection in one transaction. Pass
        // the serial numbers directly; the collection Id is supplied once.
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = nftCollection,
            SerialNumbers = new long[] { 1, 2, 3 }
        });
        Console.WriteLine($"Burn status: {receipt.Status}");
        Console.WriteLine($"Remaining circulation: {receipt.Circulation}");
        #endregion
    }
}
