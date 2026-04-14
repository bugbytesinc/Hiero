using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class GetContractBalanceTests
{
    [Test]
    public async Task Can_Get_Tinybar_Balance_For_Contract_Async()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var balance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info.Balance).IsEqualTo(balance);
    }

    [Test]
    public async Task Can_Get_Balances_For_Contract_Async()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await Assert.That(await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract)).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Missing_Payer_Account_Dos_Not_Throw_Exception()
    {
        await using var fx = await PayableContract.CreateAsync();
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = new ConsensusClient(ctx => { ctx.Endpoint = endpoint; });
        var balance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Querying_Balance_For_Non_Contract_Address_Raises_Error()
    {
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = new ConsensusClient(ctx => { ctx.Endpoint = endpoint; });
        var ex = await Assert.That(async () =>
        {
            await client.GetContractBalanceAsync(TestNetwork.Payer);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidContractId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidContractId");
    }

    [Test]
    public async Task Missing_Balance_Contract_Account_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var balance = await client.GetContractBalanceAsync(null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.Message).StartsWith("Contract Address is missing.");
    }

    [Test]
    public async Task Invalid_Contract_Address_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = new EntityId(0, 0, 0);
        var ex = await Assert.That(async () =>
        {
            var balance = await client.GetContractBalanceAsync(account);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidContractId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidContractId");
    }

    [Test]
    public async Task Invalid_Gateway_Address_Is_Ignored()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(cfg =>
        {
            cfg.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 999), cfg.Endpoint!.Uri);
        });
        var balance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(0ul);
    }

    [Test]
    public async Task Get_Contract_Balance_Requires_No_Fee()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(cfg =>
        {
            cfg.FeeLimit = 0;
        });
        var balance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(0ul);
    }

    [Test]
    public async Task Retrieving_Account_Balance_Does_Not_Create_Receipt()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var txId = client.CreateNewTransactionId();
        var balance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract, default, ctx => ctx.TransactionId = txId);
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
}
