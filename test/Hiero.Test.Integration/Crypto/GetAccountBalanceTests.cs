using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Crypto;

public class GetAccountBalanceTests
{
    [Test]
    public async Task Can_Get_Tinybar_Balance_For_Account_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = TestNetwork.Payer;
        var balance = await client.GetAccountBalanceAsync(account);
        await Assert.That(balance > 0).IsTrue();
    }

    [Test]
    public async Task Can_Get_Tinybar_Balance_For_Gateway_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = await TestNetwork.GetConsensusNodeEndpointAsync();
        var balance = await client.GetAccountBalanceAsync(account);
        await Assert.That(balance > 0).IsTrue();
    }

    [Test]
    public async Task Missing_Node_Information_Throws_Exception()
    {
        var account = TestNetwork.Payer;
        await using var client = new ConsensusClient(ctx => { ctx.Payer = TestNetwork.Payer; });
        var ex = await Assert.That(async () =>
        {
            var balance = await client.GetAccountBalanceAsync(account);
        }).ThrowsException();
        var ioe = ex as InvalidOperationException;
        await Assert.That(ioe).IsNotNull();
        await Assert.That(ioe!.Message).StartsWith("The Network Consensus Endpoint has not been configured.");
    }

    [Test]
    public async Task Missing_Payer_Account_Throws_Exception()
    {
        var account = TestNetwork.Payer;
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = new ConsensusClient(ctx => { ctx.Endpoint = endpoint; });
        var balance = await client.GetAccountBalanceAsync(account);
        await Assert.That(balance > 0).IsTrue();
    }

    [Test]
    public async Task Missing_Balance_Account_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var balance = await client.GetAccountBalanceAsync(null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.Message).StartsWith("Account Address/Alias is missing.");
    }

    [Test]
    public async Task Invalid_Account_Address_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = new EntityId(0, 0, 0);
        var ex = await Assert.That(async () =>
        {
            var balance = await client.GetAccountBalanceAsync(account);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccount");
    }

    [Test]
    public async Task Invalid_Gateway_Address_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(cfg =>
        {
            cfg.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 999), cfg.Endpoint!.Uri);
        });
        var balance = await client.GetAccountBalanceAsync(TestNetwork.Payer);
        await Assert.That(balance > 0).IsTrue();
    }

    [Test]
    public async Task Get_Account_Balance_Requires_No_Fee()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(cfg =>
        {
            cfg.FeeLimit = 0;
        });
        var balance = await client.GetAccountBalanceAsync(TestNetwork.Payer);
        await Assert.That(balance > 0).IsTrue();
    }

    [Test]
    public async Task Retrieving_Account_Balance_Is_Free()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        var account = TestNetwork.Payer;
        var balance1 = await client.GetAccountBalanceAsync(fxAccount.CreateReceipt!.Address);
        var balance2 = await client.GetAccountBalanceAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(balance1).IsEqualTo(balance2);
        await Assert.That(balance1).IsEqualTo(fxAccount.CreateParams.InitialBalance);
    }

    [Test]
    public async Task Retrieving_Account_Balance_Does_Not_Create_Receipt()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = TestNetwork.Payer;
        var txId = client.CreateNewTransactionId();
        var balance = await client.GetAccountBalanceAsync(account, default, ctx => ctx.TransactionId = txId);
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.GetReceiptAsync(txId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.TransactionId).IsEqualTo(txId);
        await Assert.That(tex.Status).IsEqualTo(ResponseCode.ReceiptNotFound);
        await Assert.That(tex.Message).StartsWith("Network failed to return a transaction receipt");
    }

    [Test]
    public async Task Can_Create_A_Topic_Async()
    {
        await using var fx = await TestTopic.CreateAsync();
        var ex = await Assert.That(async () =>
        {
            await using var client = await TestNetwork.CreateClientAsync();
            await client.GetAccountBalanceAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccountId");
    }

    [Test]
    public async Task Can_Get_Tinybar_Balance_For_Alias_Account_Async()
    {
        await using var fx = await TestAliasAccount.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var balanceByAlias = await client.GetAccountBalanceAsync(fx.Alias);
        await Assert.That(balanceByAlias > 0).IsTrue();

        var balanceByAccount = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(balanceByAccount).IsEqualTo(balanceByAlias);
    }
}
