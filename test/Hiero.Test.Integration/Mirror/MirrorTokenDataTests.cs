using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Mirror;

public class MirrorTokenDataTests
{
    [Test]
    public async Task Can_Get_Tokens_Filtered_By_Id()
    {
        // Filter the network-wide token list down to just our fresh fixture
        // token using TokenFilter.Is(...). Asserts the summary fields the
        // /tokens list endpoint returns (a reduced subset of TokenInfo).
        await using var fxToken = await TestToken.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var matches = new List<TokenSummaryData>();
        await foreach (var data in mirror.GetTokensAsync(TokenFilter.Is(fxToken.CreateReceipt!.Token)))
        {
            matches.Add(data);
            if (matches.Count >= 5)
            {
                break;
            }
        }

        await Assert.That(matches.Count).IsEqualTo(1);
        var summary = matches[0];
        await Assert.That(summary.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(summary.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(summary.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(summary.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That((uint)summary.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(summary.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
    }

    [Test]
    public async Task Can_Get_Token_Holders_Snapshot()
    {
        // The /tokens/{id}/balances endpoint reads the mirror's balance-file
        // snapshot, which the OpenAPI docs warn rolls roughly every 15 minutes.
        // A fresh token may or may not be in the most recent snapshot when this
        // test runs — so we accept either empty (token-too-fresh) or one+
        // record (treasury holding) and validate the records that do come back.
        await using var fxToken = await TestToken.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var asOf = await mirror.GetLatestConsensusTimestampAsync();

        var holders = new List<AccountBalanceData>();
        await foreach (var holder in mirror.GetTokenHoldersSnapshotAsync(fxToken.CreateReceipt!.Token, asOf))
        {
            holders.Add(holder);
            if (holders.Count >= 10)
            {
                break;
            }
        }

        TestContext.Current?.OutputWriter.WriteLine($"Snapshot returned {holders.Count} holders for the fresh token (0 is normal — snapshot rolls every ~15 min).");

        // If the snapshot has caught up, the treasury should hold the full
        // circulating supply; if it hasn't, we accept the empty list.
        if (holders.Count > 0)
        {
            var treasuryEntry = holders.FirstOrDefault(h => h.Account == fxToken.TreasuryAccount.CreateReceipt!.Address);
            await Assert.That(treasuryEntry).IsNotNull();
            await Assert.That((ulong)treasuryEntry!.Balance).IsEqualTo(fxToken.CreateParams.Circulation);
        }
    }

    [Test]
    public async Task Can_Get_Single_Nft()
    {
        await using var fxNft = await TestNft.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        // TestNft mints 3-9 metadata entries into the treasury; serial 1 is always present.
        var nftId = new Nft(fxNft.CreateReceipt!.Token, 1);
        var data = await mirror.GetNftAsync(nftId);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(data.SerialNumber).IsEqualTo(1L);
        await Assert.That(data.Owner).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(data.Deleted).IsFalse();
        await Assert.That(data.Created.Seconds > 0).IsTrue();
        await Assert.That(data.Modified.Seconds > 0).IsTrue();
        await Assert.That(data.Metadata.ToArray()).IsEquivalentTo(fxNft.Metadata[0].ToArray(),
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Can_Get_Token_Nfts()
    {
        await using var fxNft = await TestNft.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var serials = new List<NftData>();
        await foreach (var nft in mirror.GetTokenNftsAsync(fxNft.CreateReceipt!.Token))
        {
            serials.Add(nft);
        }

        await Assert.That(serials.Count).IsEqualTo(fxNft.Metadata.Length);
        foreach (var nft in serials)
        {
            await Assert.That(nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
            await Assert.That(nft.Owner).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
            await Assert.That(nft.Deleted).IsFalse();
            await Assert.That(nft.SerialNumber >= 1 && nft.SerialNumber <= fxNft.Metadata.Length).IsTrue();
        }
        // All serials present.
        var serialNumbers = serials.Select(s => s.SerialNumber).OrderBy(n => n).ToArray();
        for (int i = 0; i < serialNumbers.Length; i++)
        {
            await Assert.That(serialNumbers[i]).IsEqualTo((long)(i + 1));
        }
    }

    [Test]
    public async Task Can_Get_Account_Nfts()
    {
        await using var fxNft = await TestNft.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var holdings = new List<NftData>();
        await foreach (var nft in mirror.GetAccountNftsAsync(fxNft.TreasuryAccount.CreateReceipt!.Address,
                                                              TokenFilter.Is(fxNft.CreateReceipt!.Token)))
        {
            holdings.Add(nft);
        }

        await Assert.That(holdings.Count).IsEqualTo(fxNft.Metadata.Length);
        foreach (var nft in holdings)
        {
            await Assert.That(nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
            await Assert.That(nft.Owner).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        }
    }

    [Test]
    public async Task Can_Get_Nft_Transaction_History()
    {
        // Build up a small history on serial 1: it's minted into the treasury,
        // then transferred to a recipient, generating two history records.
        // Drop the KYC-grant key so the recipient doesn't need an explicit
        // KYC grant before receiving the transfer (matches GetNftInfoTests.cs).
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxRecipient = await TestAccount.CreateAsync();
        await fxNft.AssociateAccountsAsync(fxRecipient);
        await using var client = await TestNetwork.CreateClientAsync();

        var nftId = new Nft(fxNft.CreateReceipt!.Token, 1);
        var transferReceipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(nftId, fxNft.TreasuryAccount, fxRecipient) },
            Signatory = fxNft.TreasuryAccount.PrivateKey
        });
        await Assert.That(transferReceipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var history = new List<NftTransactionData>();
        await foreach (var entry in mirror.GetNftTransactionHistoryAsync(nftId))
        {
            history.Add(entry);
        }

        // Newest-first by default — so [0] is the transfer, [1] is the mint.
        await Assert.That(history.Count >= 2).IsTrue();
        var transfer = history[0];
        await Assert.That(transfer.TransactionType).IsEqualTo("CRYPTOTRANSFER");
        await Assert.That(transfer.Sender).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(transfer.Receiver).IsEqualTo(fxRecipient.CreateReceipt!.Address);
        await Assert.That(transfer.Consensus.Seconds > 0).IsTrue();

        var mint = history.Last();
        await Assert.That(mint.TransactionType).IsEqualTo("TOKENMINT");
        await Assert.That(mint.Sender).IsNull();
        await Assert.That(mint.Receiver).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
    }
}
