using Hiero.Mirror;

namespace Hiero.Test.Integration.Fixtures;

public static class AssertHg
{
    public static async Task TokenKycStatusAsync(TestToken fxToken, TestAccount fxAccount, TokenKycStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.KycStatus).IsEqualTo(status);
    }

    public static async Task TokenKycStatusAsync(TestToken fxToken, TestAliasAccount fxAccount, TokenKycStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.KycStatus).IsEqualTo(status);
    }

    public static async Task NftKycStatusAsync(TestNft fxNft, TestAccount fxAccount, TokenKycStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.KycStatus).IsEqualTo(status);
    }

    public static async Task NftKycStatusAsync(TestNft fxNft, TestAliasAccount fxAccount, TokenKycStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.KycStatus).IsEqualTo(status);
    }

    public static async Task TokenTradableStatusAsync(TestToken fxToken, TestAccount fxAccount, TokenTradableStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.FreezeStatus).IsEqualTo(status);
    }

    public static async Task TokenTradableStatusAsync(TestToken fxToken, TestAliasAccount fxAccount, TokenTradableStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.FreezeStatus).IsEqualTo(status);
    }

    public static async Task NftTradableStatusAsync(TestNft fxNft, TestAccount fxAccount, TokenTradableStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.FreezeStatus).IsEqualTo(status);
    }

    public static async Task NftTradableStatusAsync(TestNft fxNft, TestAliasAccount fxAccount, TokenTradableStatus status)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var tokenRecord = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.FreezeStatus).IsEqualTo(status);
    }

    public static async Task TokenPausedAsync(TestToken fxToken, TokenTradableStatus status)
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.PauseStatus).IsEqualTo(status);
    }

    public static async Task NftPausedAsync(TestNft fxNft, TokenTradableStatus status)
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.PauseStatus).IsEqualTo(status);
    }

    public static async Task TokenBalanceAsync(TestToken fxToken, TestAccount fxAccount, ulong expectedBalance)
    {
        var balance = await fxAccount.GetTokenBalanceAsync(fxToken);
        await Assert.That(balance).IsEqualTo((long)expectedBalance);
    }

    public static async Task NftBalanceAsync(TestNft fxNft, TestAccount fxAccount, ulong expectedBalance)
    {
        var balance = await fxAccount.GetTokenBalanceAsync(fxNft);
        await Assert.That(balance).IsEqualTo((long)expectedBalance);
    }

    public static async Task NftBalanceAsync(TestNft fxNft, TestAccount fxAccount, int expectedBalance)
    {
        var balance = await fxAccount.GetTokenBalanceAsync(fxNft);
        await Assert.That(balance).IsEqualTo(expectedBalance);
    }

    public static async Task CryptoBalanceAsync(TestAccount fxAccount, ulong expectedBalance)
    {
        var balance = await fxAccount.GetCryptoBalanceAsync();
        await Assert.That(balance).IsEqualTo(expectedBalance);
    }

    public static Task CryptoBalanceAsync(TestAccount fxAccount, int expectedBalance)
    {
        return CryptoBalanceAsync(fxAccount, (ulong)expectedBalance);
    }

    public static async Task CryptoContractBalanceAsync(PayableContract fxContract, ulong expectedBalance)
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var balance = await client.GetContractBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(expectedBalance);
    }

    public static async Task TokenNotAssociatedAsync(TestToken fxToken, TestAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNull();
    }

    public static async Task TokenNotAssociatedAsync(TestToken fxToken, TestAliasAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNull();
    }

    public static async Task<TokenHoldingData> TokenIsAssociatedAsync(TestToken fxToken, TestAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        return association!;
    }

    public static async Task<TokenHoldingData> TokenIsAssociatedAsync(TestToken fxToken, TestAliasAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        return association!;
    }

    public static async Task NftNotAssociatedAsync(TestNft fxNft, TestAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(association).IsNull();
    }

    public static async Task NftNotAssociatedAsync(TestNft fxNft, TestAliasAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(association).IsNull();
    }

    public static async Task<TokenHoldingData> NftIsAssociatedAsync(TestNft fxNft, TestAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        return association!;
    }

    public static async Task<TokenHoldingData> NftIsAssociatedAsync(TestNft fxNft, TestAliasAccount fxAccount)
    {
        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).IsNotNull();
        var association = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        return association!;
    }

    public static void ContainsRoyalty(TestToken fxToken, TestAccount fxPayer, TestAccount fxReceiver, int amount, IReadOnlyList<RoyaltyTransfer> royalties)
    {
        var token = fxToken.CreateReceipt.Token;
        var payer = fxPayer.CreateReceipt.Address;
        var receiver = fxReceiver.CreateReceipt.Address;
        foreach (var entry in royalties)
        {
            if (amount == entry.Amount && token == entry.Token && receiver == entry.Receiver && entry.Payers.Contains(payer))
            {
                return;
            }
        }
        throw new AssertionException($"Unable to find royalty payment using token {token} involving a payer {payer} to receiver {receiver} with amount {amount}.");
    }

    public static void ContainsHbarRoyalty(TestAccount fxPayer, TestAccount fxReceiver, int amount, IReadOnlyList<RoyaltyTransfer> royalties)
    {
        var payer = fxPayer.CreateReceipt!.Address;
        var receiver = fxReceiver.CreateReceipt!.Address;
        foreach (var entry in royalties)
        {
            if (amount == entry.Amount && EntityId.None == entry.Token && receiver == entry.Receiver && entry.Payers.Contains(payer))
            {
                return;
            }
        }
        throw new AssertionException($"Unable to find royalty payment using hBar involving a payer {payer} to receiver {receiver} with amount {amount}.");
    }

    public static void SingleAssociation(TestToken fxToken, TestAccount fxAccount, IReadOnlyList<Association> associations)
    {
        if (associations is not { Count: 1 } ||
            fxToken.CreateReceipt!.Token != associations[0].Token ||
            fxAccount.CreateReceipt!.Address != associations[0].Holder)
        {
            throw new AssertionException($"Unable to find single association record using token {fxToken.CreateReceipt!.Token} with account {fxAccount.CreateReceipt!.Address}.");
        }
    }

    public static void SingleAssociation(TestNft fxNft, TestAccount fxAccount, IReadOnlyList<Association> associations)
    {
        if (associations is not { Count: 1 } ||
            fxNft.CreateReceipt!.Token != associations[0].Token ||
            fxAccount.CreateReceipt!.Address != associations[0].Holder)
        {
            throw new AssertionException($"Unable to find single association record using NFT {fxNft.CreateReceipt!.Token} with account {fxAccount.CreateReceipt!.Address}.");
        }
    }

    public static void SemanticVersionGreaterOrEqualThan(SemanticVersion expected, SemanticVersion actual)
    {
        if (expected.Major > actual.Major ||
            (expected.Major == actual.Major && expected.Minor > actual.Minor) ||
            (expected.Major == actual.Major && expected.Minor == actual.Minor && expected.Patch > actual.Patch))
        {
            throw new AssertionException($"Semantic Version {actual.Major}.{actual.Minor}.{actual.Patch} is not greater than {expected.Major}.{expected.Minor}.{expected.Patch}");
        }
    }

    public static async Task EmptyAsync(ReadOnlyMemory<byte> value)
    {
        await Assert.That(value.IsEmpty).IsTrue();
    }

    public static async Task NotEmptyAsync(ReadOnlyMemory<byte> value)
    {
        await Assert.That(value.IsEmpty).IsFalse();
    }

}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
