using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using Google.Protobuf;
using Proto;

namespace Hiero.Test.Integration.Network;

public class ExternalQueryTests
{
    // Defect 0.49.0
    [Test]
    public async Task Get_Receipt_Via_External_Query_Fails_Defect()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var query = new Query
        {
            TransactionGetReceipt = new TransactionGetReceiptQuery
            {
                TransactionID = new TransactionID(fxAccount.CreateReceipt!.TransactionId)
            }
        };

        var result = await client.QueryExternalAsync(query.ToByteArray());
        await Assert.That(result.IsEmpty).IsFalse();

        var response = Response.Parser.ParseFrom(result.Span);
        await Assert.That(response).IsNotNull();
        await Assert.That(response.TransactionGetReceipt.Receipt).IsNull();
    }

    [Test]
    public async Task Can_Get_Account_Info_Via_External_Query()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var query = new Query
        {
            CryptoGetInfo = new CryptoGetInfoQuery
            {
                AccountID = new AccountID(fxAccount.CreateReceipt!.Address)
            }
        };

        var result = await client.QueryExternalAsync(query.ToByteArray());
        await Assert.That(result.IsEmpty).IsFalse();

        var response = Response.Parser.ParseFrom(result.Span);
        await Assert.That(response).IsNotNull();

        var info = response.CryptoGetInfo.AccountInfo;
        await Assert.That(info).IsNotNull();

        await Assert.That(info.AccountID.AsAddress()).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Invalid_Query_Still_Produces_Result()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var query = new Query
        {
            CryptoGetInfo = new CryptoGetInfoQuery
            {
                AccountID = new AccountID(EntityId.None)
            }
        };

        var result = await client.QueryExternalAsync(query.ToByteArray());
        await Assert.That(result.IsEmpty).IsFalse();

        var response = Response.Parser.ParseFrom(result.Span);
        await Assert.That(response).IsNotNull();
        await Assert.That(response.CryptoGetInfo.Header.NodeTransactionPrecheckCode).IsEqualTo(ResponseCodeEnum.InvalidAccountId);
        await Assert.That(response.CryptoGetInfo.AccountInfo).IsNull();
    }

    [Test]
    public async Task Invalid_Receipt_Request_Still_Produces_Result()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var query = new Query
        {
            TransactionGetReceipt = new TransactionGetReceiptQuery()
        };

        var result = await client.QueryExternalAsync(query.ToByteArray());
        await Assert.That(result.IsEmpty).IsFalse();

        var response = Response.Parser.ParseFrom(result.Span);
        await Assert.That(response).IsNotNull();
        await Assert.That(response.TransactionGetReceipt.Receipt).IsNull();
        await Assert.That(response.ResponseHeader!.NodeTransactionPrecheckCode).IsEqualTo(ResponseCodeEnum.InvalidTransactionId);
    }
}
