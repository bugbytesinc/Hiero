using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class ContractBytecodeTests
{
    [Test]
    public async Task Can_Get_Stateless_Contract_Bytecode()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var bytecode = await client.GetContractBytecodeAsync(fx.ContractReceipt!.Contract);
        await Assert.That(bytecode.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Can_Get_Stateful_Contract_Bytecode()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var bytecode = await client.GetContractBytecodeAsync(fx.ContractReceipt!.Contract);
        await Assert.That(bytecode.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Can_Get_Event_Emitting_Contract_Bytecode()
    {
        await using var fx = await EventEmittingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var bytecode = await client.GetContractBytecodeAsync(fx.ContractReceipt!.Contract);
        await Assert.That(bytecode.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Can_Get_Event_Payable_Contract_Bytecode()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var grpcBytecode = await client.GetContractBytecodeAsync(fx.ContractReceipt!.Contract);
        await Assert.That(grpcBytecode.IsEmpty).IsFalse();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var mirrorBytecode = (await mirror.GetContractAsync(fx.ContractReceipt!.Contract))!.RuntimeBytecode;
        await Assert.That(mirrorBytecode.ToArray()).IsEquivalentTo(grpcBytecode.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Get_Non_Existant_Contract_From_Token_Id_Raises_Error()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var grpcBytecode = await client.GetContractBytecodeAsync(fx.CreateReceipt!.Token);
        await Assert.That(grpcBytecode.IsEmpty).IsFalse();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        await Assert.That(await mirror.GetContractAsync(fx.CreateReceipt!.Token)).IsNull();
    }

    [Test]
    public async Task Retrieving_Non_Existent_Contract_Bytecode_From_Account_ID_Returns_Empty_Bytecode()
    {
        // Consensus node v0.77 returns no bytecode for an account that is not a
        // contract (unless it carries an EIP-7702 code delegation, which yields
        // the delegation indicator). Earlier versions returned the HIP-906
        // account proxy (redirect) bytecode. Tokens and schedules still return
        // their facade bytecode.
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var grpcBytecode = await client.GetContractBytecodeAsync(fx.CreateReceipt!.Address);
        await Assert.That(grpcBytecode.IsEmpty).IsTrue();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        await Assert.That(await mirror.GetContractAsync(fx.CreateReceipt!.Address)).IsNull();
    }

    [Test]
    public async Task Retrieving_Non_Existent_Contract_Bytecode_From_Topic_ID_Returns_Default_Bytecode()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GetContractBytecodeAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidContractId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidContractId");
    }

    [Test]
    public async Task Payer_Account_Has_No_Bytecode()
    {
        // See Retrieving_Non_Existent_Contract_Bytecode_From_Account_ID_Returns_Empty_Bytecode:
        // since consensus node v0.77 a plain account has no bytecode.
        await using var client = await TestNetwork.CreateClientAsync();
        var grpcBytecode = await client.GetContractBytecodeAsync(TestNetwork.Payer);
        await Assert.That(grpcBytecode.IsEmpty).IsTrue();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        await Assert.That(await mirror.GetContractAsync(TestNetwork.Payer)).IsNull();
    }
}
