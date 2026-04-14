using Hiero.Test.Helpers;
using System.Text;

namespace Hiero.Test.Integration.Fixtures;

public class PayableContract : IAsyncDisposable
{
    public required CreateFileParams FileParams;
    public required FileReceipt FileReceipt;
    public required CreateContractParams ContractParams;
    public required CreateContractReceipt ContractReceipt;
    public required ReadOnlyMemory<byte> PublicKey;
    public required ReadOnlyMemory<byte> PrivateKey;

    /// <summary>
    /// The contract 'bytecode' encoded in Hex, Same as hello_world from java sdk, compiled in Remix for with Solidity 0.5.4
    /// </summary>
    private const string PAYABLE_CONTRACT_BYTECODE = "0x6080604052336000806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff16021790555061032a806100536000396000f3fe608060405260043610610051576000357c01000000000000000000000000000000000000000000000000000000009004806341c0e1b514610053578063c1cfb99a1461006a578063e264d18314610095575b005b34801561005f57600080fd5b506100686100e6565b005b34801561007657600080fd5b5061007f6101a6565b6040518082815260200191505060405180910390f35b3480156100a157600080fd5b506100e4600480360360208110156100b857600080fd5b81019080803573ffffffffffffffffffffffffffffffffffffffff1690602001909291905050506101c5565b005b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614151561018d576040517f08c379a000000000000000000000000000000000000000000000000000000000815260040180806020018281038252602b8152602001806102d4602b913960400191505060405180910390fd5b3373ffffffffffffffffffffffffffffffffffffffff16ff5b60003073ffffffffffffffffffffffffffffffffffffffff1631905090565b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff1614151561026c576040517f08c379a000000000000000000000000000000000000000000000000000000000815260040180806020018281038252602b8152602001806102d4602b913960400191505060405180910390fd5b60003073ffffffffffffffffffffffffffffffffffffffff163190508173ffffffffffffffffffffffffffffffffffffffff166108fc829081150290604051600060405180830381858888f193505050501580156102ce573d6000803e3d6000fd5b50505056fe4f6e6c7920636f6e7472616374206f776e65722063616e2063616c6c20746869732066756e6374696f6e2ea165627a7a72305820958b3369a655d57506babce1f72e76d752de46eed693cc101284b76522f404170029";

    public static async Task<PayableContract> CreateAsync(Action<PayableContract>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Payable Contract Instance");
        var (publicKey, privateKey) = Generator.KeyPair();
        var fileParams = new CreateFileParams
        {
            Expiration = DateTime.UtcNow.AddSeconds(7890000),
            Endorsements = [TestNetwork.Endorsement],
            Contents = Encoding.UTF8.GetBytes(PAYABLE_CONTRACT_BYTECODE)
        };
        await using var client = await TestNetwork.CreateClientAsync();
        var fileReceipt = await client.CreateFileAsync(fileParams, ctx => ctx.Memo = Generator.Code(20));
        var contractParams = new CreateContractParams
        {
            File = fileReceipt.File,
            Administrator = publicKey,
            Signatory = privateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            InitialBalance = 1_000_000,
            RenewPeriod = TimeSpan.FromSeconds(7890000),
        };
        var fixture = new PayableContract
        {
            FileParams = fileParams,
            FileReceipt = fileReceipt,
            ContractParams = contractParams,
            ContractReceipt = null!,
            PublicKey = publicKey,
            PrivateKey = privateKey,
        };
        customize?.Invoke(fixture);
        fixture.ContractReceipt = await client.CreateContractAsync(fixture.ContractParams, ctx =>
        {
            ctx.Memo = Generator.Code(20);
        });
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Payable Contract Instance Created");
        return fixture;
    }
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    public static implicit operator EntityId(PayableContract fixture)
    {
        return fixture.ContractReceipt.Contract;
    }
}
