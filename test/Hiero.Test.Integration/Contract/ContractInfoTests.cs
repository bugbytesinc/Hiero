using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Contract;

public class ContractInfoTests
{
    [Test]
    public async Task Can_Get_Stateless_Contract_Info()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.EvmAddress).IsNotNull();
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.Expiration > ConsensusTimeStamp.Now).IsTrue();
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Size >= 0 && info.Size <= fx.FileParams.Contents.Length).IsTrue();
        await Assert.That(info.Memo).IsEqualTo(fx.ContractParams.Memo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Get_Stateful_Contract_Info()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.EvmAddress).IsNotNull();
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.Expiration > ConsensusTimeStamp.Now).IsTrue();
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Size >= 0 && info.Size <= fx.FileParams.Contents.Length).IsTrue();
        await Assert.That(info.Memo).IsEqualTo(fx.ContractParams.Memo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Get_Non_Existant_Contract_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetContractInfoAsync(fx.CreateReceipt!.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidContractId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidContractId");
    }

    [Test]
    public async Task Can_Get_Imutable_Stateful_Contract_Info()
    {
        await using var fx = await StatefulContract.CreateAsync(f =>
        {
            f.ContractParams.Administrator = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.EvmAddress).IsNotNull();
        // Immutable Contracts list their "contract" key as the administrator Key.
        await Assert.That(info.Administrator!.Type).IsEqualTo(KeyType.Contract);
        await Assert.That(info.Administrator.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Expiration > ConsensusTimeStamp.Now).IsTrue();
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Size >= 0 && info.Size <= fx.FileParams.Contents.Length).IsTrue();
        await Assert.That(info.Memo).StartsWith(fx.ContractParams.Memo!);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }
}
