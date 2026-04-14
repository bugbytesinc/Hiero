using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class InitCodeContract : IAsyncDisposable
{
    public required CreateContractParams ContractParams;
    public required CreateContractReceipt ContractReceipt;
    public required ReadOnlyMemory<byte> PublicKey;
    public required ReadOnlyMemory<byte> PrivateKey;

    /// <summary>
    /// The contract 'bytecode' encoded in Hex, Same as hello_world from java sdk, compiled in Remix for with Solidity 0.5.4
    /// </summary>
    public const string CONTRACT_BYTECODE = "608060405234801561001057600080fd5b50336000806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506101be806100606000396000f3fe608060405234801561001057600080fd5b5060043610610053576000357c01000000000000000000000000000000000000000000000000000000009004806341c0e1b514610058578063cfae321714610062575b600080fd5b6100606100e5565b005b61006a610155565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156100aa57808201518184015260208101905061008f565b50505050905090810190601f1680156100d75780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161415610153573373ffffffffffffffffffffffffffffffffffffffff16ff5b565b60606040805190810160405280600d81526020017f48656c6c6f2c20776f726c64210000000000000000000000000000000000000081525090509056fea165627a7a7230582077fbec49f64eda23cb526275088f65c1fc7e8d002b4681e098f18292791cd94b0029";

    public static async Task<InitCodeContract> CreateAsync(Action<InitCodeContract>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: InitCode Contract Instance");
        var (publicKey, privateKey) = Generator.KeyPair();
        var contractParams = new CreateContractParams
        {
            ByteCode = Hex.ToBytes(CONTRACT_BYTECODE),
            Administrator = publicKey,
            Signatory = privateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            RenewPeriod = TimeSpan.FromSeconds(7890000),
        };
        var fixture = new InitCodeContract
        {
            ContractParams = contractParams,
            ContractReceipt = null!,
            PublicKey = publicKey,
            PrivateKey = privateKey,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.ContractReceipt = await client.CreateContractAsync(fixture.ContractParams, ctx =>
        {
            ctx.Memo = Generator.Code(20);
        });
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: InitCode Contract Instance Created");
        return fixture;
    }
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    public static implicit operator EntityId(InitCodeContract fixture)
    {
        return fixture.ContractReceipt.Contract;
    }
}
