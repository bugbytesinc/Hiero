using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Text;

namespace Hiero.Test.Integration.Record;

public class GetReceiptTests
{
    [Test]
    public async Task Can_Get_Create_Topic_Receipt()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.GetReceiptAsync(fx.CreateReceipt!.TransactionId);
        var topicReceipt = receipt as CreateTopicReceipt;
        await Assert.That(topicReceipt).IsNotNull();
        await Assert.That(topicReceipt!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(topicReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(topicReceipt.Topic).IsEqualTo(fx.CreateReceipt.Topic);
    }

    [Test]
    public async Task Can_Get_Submit_Message_Receipt()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var receipt1 = await client.SubmitMessageAsync(fx.CreateReceipt!.Topic, message, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fx.ParticipantPrivateKey);
        });
        var receipt2 = await client.GetReceiptAsync(receipt1.TransactionId);
        var sendReceipt = receipt2 as SubmitMessageReceipt;
        await Assert.That(sendReceipt).IsNotNull();
        await Assert.That(sendReceipt!.TransactionId).IsEqualTo(receipt1.TransactionId);
        await Assert.That(sendReceipt.Status).IsEqualTo(receipt1.Status);
        await Assert.That(sendReceipt.SequenceNumber).IsEqualTo(receipt1.SequenceNumber);
        await Assert.That(sendReceipt.RunningHash.ToArray()).IsEquivalentTo(receipt1.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(sendReceipt.RunningHashVersion).IsEqualTo(receipt1.RunningHashVersion);
    }

    [Test]
    public async Task Can_Get_Create_Contract_Receipt()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.GetReceiptAsync(fx.ContractReceipt!.TransactionId);
        var createReceipt = receipt as CreateContractReceipt;
        await Assert.That(createReceipt).IsNotNull();
        await Assert.That(createReceipt!.TransactionId).IsEqualTo(fx.ContractReceipt.TransactionId);
        await Assert.That(createReceipt.Status).IsEqualTo(fx.ContractReceipt.Status);
        await Assert.That(createReceipt.Contract).IsEqualTo(fx.ContractReceipt.Contract);
    }

    [Test]
    public async Task Can_Get_Create_Account_Receipt()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.GetReceiptAsync(fx.CreateReceipt!.TransactionId);
        var accountReceipt = receipt as CreateAccountReceipt;
        await Assert.That(accountReceipt).IsNotNull();
        await Assert.That(accountReceipt!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(accountReceipt.Status).IsEqualTo(fx.CreateReceipt.Status);
        await Assert.That(accountReceipt.Address).IsEqualTo(fx.CreateReceipt.Address);
    }

    [Test]
    public async Task Can_Get_Create_File_Receipt()
    {
        await using var fx = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.GetReceiptAsync(fx.CreateReceipt!.TransactionId);
        var fileReceipt = receipt as FileReceipt;
        await Assert.That(fileReceipt).IsNotNull();
        await Assert.That(fileReceipt!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(fileReceipt.Status).IsEqualTo(fx.CreateReceipt.Status);
        await Assert.That(fileReceipt.File).IsEqualTo(fx.CreateReceipt.File);
    }

    [Test]
    public async Task Can_Get_Create_Token_Receipt()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.GetReceiptAsync(fx.CreateReceipt!.TransactionId);
        var createReceipt = receipt as CreateTokenReceipt;
        await Assert.That(createReceipt).IsNotNull();
        await Assert.That(createReceipt!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(createReceipt.Status).IsEqualTo(fx.CreateReceipt.Status);
        await Assert.That(createReceipt.Token).IsEqualTo(fx.CreateReceipt.Token);
    }

    [Test]
    public async Task Can_Get_List_Of_One_Receipt()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, new EntityId(0, 0, 800), 1);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        await Assert.That(receipts.Count).IsEqualTo(1);
        await Assert.That(receipts[0].Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Get_List_Of_No_Receipts()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var txid = client.CreateNewTransactionId();
        var receipts = await client.GetAllReceiptsAsync(txid);
        await Assert.That(receipts.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Get_Create_Token_Receipt_As_List()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipts = await client.GetAllReceiptsAsync(fx.CreateReceipt!.TransactionId);
        await Assert.That(receipts.Count).IsEqualTo(1);
        var createReceipt = receipts[0] as CreateTokenReceipt;
        await Assert.That(createReceipt).IsNotNull();
        await Assert.That(createReceipt!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(createReceipt.Status).IsEqualTo(fx.CreateReceipt.Status);
        await Assert.That(createReceipt.Token).IsEqualTo(fx.CreateReceipt.Token);
    }

    [Test]
    public async Task Can_Get_Token_Receipt_For_Burn()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();

        var amountToDestroy = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestroy;

        var originalReceipt = await client.BurnTokensAsync(fxToken, amountToDestroy, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.SupplyPrivateKey);
        });
        await Assert.That(originalReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(originalReceipt.Circulation).IsEqualTo(expectedCirculation);

        var copyReceipt = await client.GetReceiptAsync(originalReceipt.TransactionId);
        var tokenReceipt = copyReceipt as TokenReceipt;
        await Assert.That(tokenReceipt).IsNotNull();
        await Assert.That(tokenReceipt!.TransactionId).IsEqualTo(originalReceipt.TransactionId);
        await Assert.That(tokenReceipt.Status).IsEqualTo(originalReceipt.Status);
        await Assert.That(tokenReceipt.Circulation).IsEqualTo(originalReceipt.Circulation);
    }

    [Test]
    public async Task Can_Get_Token_Receipt_For_Mint()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();

        var originalReceipt = await client.MintTokenAsync(fxToken.CreateReceipt.Token, fxToken.CreateParams.Circulation, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.SupplyPrivateKey);
        });
        await Assert.That(originalReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(originalReceipt.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);

        var copyReceipt = await client.GetReceiptAsync(originalReceipt.TransactionId);
        var tokenReceipt = copyReceipt as TokenReceipt;
        await Assert.That(tokenReceipt).IsNotNull();
        await Assert.That(tokenReceipt!.TransactionId).IsEqualTo(originalReceipt.TransactionId);
        await Assert.That(tokenReceipt.Status).IsEqualTo(originalReceipt.Status);
        await Assert.That(tokenReceipt.Circulation).IsEqualTo(originalReceipt.Circulation);
    }

    [Test]
    public async Task Can_Get_Token_Receipt_For_Confiscate()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2 * fxToken.CreateParams.Circulation / (ulong)Generator.Integer(3, 5);
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken, fxToken.TreasuryAccount, -(long)xferAmount),
                new TokenTransfer(fxToken, fxAccount, (long)xferAmount)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        var originalReceipt = await client.ConfiscateTokensAsync(fxToken, fxAccount, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.ConfiscatePrivateKey);
        });
        await Assert.That(originalReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(originalReceipt.Circulation).IsEqualTo(expectedTreasury);

        var copyReceipt = await client.GetReceiptAsync(originalReceipt.TransactionId);
        var tokenReceipt = copyReceipt as TokenReceipt;
        await Assert.That(tokenReceipt).IsNotNull();
        await Assert.That(tokenReceipt!.TransactionId).IsEqualTo(originalReceipt.TransactionId);
        await Assert.That(tokenReceipt.Status).IsEqualTo(originalReceipt.Status);
        await Assert.That(tokenReceipt.Circulation).IsEqualTo(originalReceipt.Circulation);
    }
}
