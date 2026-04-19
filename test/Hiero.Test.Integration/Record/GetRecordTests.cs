using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Text;

namespace Hiero.Test.Integration.Record;

public class GetRecordTests
{
    [Test]
    public async Task Can_Get_Transaction_Record()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = Generator.Integer(20, 30);
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, amount);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.TransactionId).IsEqualTo(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
    }

    [Test]
    public async Task Empty_Transaction_Id_Throws_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.GetTransactionRecordAsync(null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("transaction");
        await Assert.That(ane.Message).StartsWith("Transaction is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Can_Get_Create_Topic_Record()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var record = await client.GetTransactionRecordAsync(fx.CreateReceipt!.TransactionId);
        var topicRecord = record as CreateTopicRecord;
        await Assert.That(topicRecord).IsNotNull();
        await Assert.That(topicRecord!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(topicRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(topicRecord.Topic).IsEqualTo(fx.CreateReceipt.Topic);
    }

    [Test]
    public async Task Can_Get_Submit_Message_Record()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var submitReceipt = await client.SubmitMessageAsync(fx.CreateReceipt!.Topic, message, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fx.ParticipantPrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(submitReceipt.TransactionId);
        var submitRecord = record as SubmitMessageRecord;
        await Assert.That(submitRecord).IsNotNull();
        await Assert.That(submitRecord!.TransactionId).IsEqualTo(submitReceipt.TransactionId);
        await Assert.That(submitRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(submitRecord.SequenceNumber).IsEqualTo(submitReceipt.SequenceNumber);
        await Assert.That(submitRecord.RunningHash.ToArray()).IsEquivalentTo(submitReceipt.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(submitRecord.RunningHashVersion).IsEqualTo(submitReceipt.RunningHashVersion);
    }

    [Test]
    public async Task Can_Get_Create_Contract_Record()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var record = await client.GetTransactionRecordAsync(fx.ContractReceipt!.TransactionId);
        var createRecord = record as CreateContractRecord;
        await Assert.That(createRecord).IsNotNull();
        await Assert.That(createRecord!.TransactionId).IsEqualTo(fx.ContractReceipt.TransactionId);
        await Assert.That(createRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(createRecord.Contract).IsEqualTo(fx.ContractReceipt.Contract);
    }

    [Test]
    public async Task Can_Get_Create_Account_Record()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var record = await client.GetTransactionRecordAsync(fx.CreateReceipt!.TransactionId);
        var accountRecord = record as CreateAccountRecord;
        await Assert.That(accountRecord).IsNotNull();
        await Assert.That(accountRecord!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(accountRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(accountRecord.Address).IsEqualTo(fx.CreateReceipt.Address);
    }

    [Test]
    public async Task Can_Get_Create_File_Record()
    {
        await using var fx = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var record = await client.GetTransactionRecordAsync(fx.CreateReceipt!.TransactionId);
        var fileRecord = record as FileRecord;
        await Assert.That(fileRecord).IsNotNull();
        await Assert.That(fileRecord!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(fileRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(fileRecord.File).IsEqualTo(fx.CreateReceipt.File);
    }

    [Test]
    public async Task Can_Get_Create_Token_Record()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var record = await client.GetTransactionRecordAsync(fx.CreateReceipt!.TransactionId);
        var createRecord = record as CreateTokenRecord;
        await Assert.That(createRecord).IsNotNull();
        await Assert.That(createRecord!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(createRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(createRecord.Token).IsEqualTo(fx.CreateReceipt.Token);
    }

    [Test]
    public async Task Can_Get_List_Of_One_Record()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, new EntityId(0, 0, 800), 1);
        var records = await client.GetAllTransactionRecordsAsync(receipt.TransactionId);
        await Assert.That(records.Count).IsEqualTo(1);
        await Assert.That(records[0].Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Get_List_Of_No_Records()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var txid = client.CreateNewTransactionId();
        var records = await client.GetAllTransactionRecordsAsync(txid);
        await Assert.That(records.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Get_Create_Token_Record_As_List()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var records = await client.GetAllTransactionRecordsAsync(fx.CreateReceipt!.TransactionId);
        await Assert.That(records.Count).IsEqualTo(1);
        var createRecord = records[0] as CreateTokenRecord;
        await Assert.That(createRecord).IsNotNull();
        await Assert.That(createRecord!.TransactionId).IsEqualTo(fx.CreateReceipt.TransactionId);
        await Assert.That(createRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(createRecord.Token).IsEqualTo(fx.CreateReceipt.Token);
    }

    [Test]
    public async Task Can_Get_Token_Record_For_Burn()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amountToDestroy = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestroy;

        var burnReceipt = await client.BurnTokenAsync(fxToken, amountToDestroy, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.SupplyPrivateKey);
        });
        await Assert.That(burnReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(burnReceipt.Circulation).IsEqualTo(expectedCirculation);

        var record = await client.GetTransactionRecordAsync(burnReceipt.TransactionId);
        var tokenRecord = record as TokenRecord;
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(tokenRecord.Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Can_Get_Token_Record_For_Mint()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var mintReceipt = await client.MintTokenAsync(fxToken.CreateReceipt!.Token, fxToken.CreateParams.Circulation, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.SupplyPrivateKey);
        });
        await Assert.That(mintReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(mintReceipt.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);

        var record = await client.GetTransactionRecordAsync(mintReceipt.TransactionId);
        var tokenRecord = record as TokenRecord;
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(tokenRecord.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);
    }

    [Test]
    public async Task Can_Get_Token_Record_For_Confiscate()
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

        var confiscateReceipt = await client.ConfiscateTokenAsync(fxToken, fxAccount, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.ConfiscatePrivateKey);
        });
        await Assert.That(confiscateReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(confiscateReceipt.Circulation).IsEqualTo(expectedTreasury);

        var record = await client.GetTransactionRecordAsync(confiscateReceipt.TransactionId);
        var tokenRecord = record as TokenRecord;
        await Assert.That(tokenRecord).IsNotNull();
        await Assert.That(tokenRecord!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(tokenRecord.Circulation).IsEqualTo(expectedTreasury);
    }
}
