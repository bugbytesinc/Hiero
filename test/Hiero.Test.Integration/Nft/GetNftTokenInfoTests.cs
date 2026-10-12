using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.NftTokens;

public class GetNftTokenInfoTests
{
    [Test]
    public async Task Can_Get_Nft_Token_Info()
    {
        await using var fx = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await Assert.That(fx.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fx.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fx.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fx.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fx.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fx.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fx.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fx.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fx.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fx.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fx.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fx.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fx.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Support_For_Defect2088_Supply_Key_Not_Being_Recorded_Reproduce_Non_Reproduce_Defect()
    {
        // Defect 0.18.0
        await using var fxTreasury = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (supplyPublicKey, supplyPrivateKey) = Generator.KeyPair();
        var (royaltyPublicKey, royaltyPrivateKey) = Generator.KeyPair();
        var createParams = new CreateNftParams
        {
            Name = "012345678912",
            Symbol = "ABCD",
            Treasury = fxTreasury,
            Administrator = adminPublicKey,
            SupplyEndorsement = supplyPublicKey,
            RoyaltiesEndorsement = royaltyPublicKey,
            Expiration = DateTime.UtcNow.AddSeconds(7890000),
            Signatory = new Signatory(fxTreasury, adminPrivateKey)
        };
        var receipt = await client.CreateNftAsync(createParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(receipt.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(createParams.RoyaltiesEndorsement);
    }

    [Test]
    public async Task Support_For_Defect2088_Supply_Key_Not_Being_Recorded_Demonstrate_Problem_Defect()
    {
        // Defect 0.18.0
        await using var fxTreasury = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (supplyPublicKey, supplyPrivateKey) = Generator.KeyPair();
        var (royaltyPublicKey, royaltyPrivateKey) = Generator.KeyPair();
        var createParams = new CreateNftParams
        {
            Name = "012345678912",
            Symbol = "ABCD",
            Treasury = fxTreasury,
            Administrator = adminPublicKey,
            SupplyEndorsement = supplyPublicKey,
            RoyaltiesEndorsement = royaltyPublicKey,
            Expiration = DateTime.UtcNow.AddSeconds(7890000),
            Signatory = new Signatory(fxTreasury, adminPrivateKey)
        };
        var receipt = await client.CreateNftAsync(createParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var metadata = Enumerable.Range(1, 10).Select(_ => Generator.SHA384Hash()).ToArray();
        var mintReceipt = await client.MintNftsAsync(new MintNftParams
        {
            Token = receipt.Token,
            Metadata = metadata,
            Signatory = supplyPrivateKey,
        });
        await Assert.That(mintReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(receipt.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(createParams.RoyaltiesEndorsement);
    }

    [Test]
    public async Task Null_Token_Identifier_Raises_Exception()
    {
        await using var fx = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetTokenInfoAsync(null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null");
    }

    [Test]
    public async Task Empty_Address_Identifier_Raises_Exception()
    {
        await using var fx = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetTokenInfoAsync(EntityId.None);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null");
    }

    [Test]
    public async Task Account_Address_For_Token_Symbol_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetTokenInfoAsync(fx.CreateReceipt!.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidTokenId");
    }

    [Test]
    public async Task Contract_Address_For_Token_Symbol_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetTokenInfoAsync(fx.ContractReceipt!.Contract);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidTokenId");
    }

    [Test]
    public async Task Topic_Address_For_Token_Symbol_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetTokenInfoAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidTokenId");
    }

    [Test]
    public async Task File_Address_For_Token_Symbol_Raises_Error()
    {
        await using var fx = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetTokenInfoAsync(fx.CreateReceipt!.File);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidTokenId");
    }
}
