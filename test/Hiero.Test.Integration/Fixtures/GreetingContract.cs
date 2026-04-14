using Hiero.Mirror;
using Hiero.Test.Helpers;
using System.Text;

namespace Hiero.Test.Integration.Fixtures;

public class GreetingContract : IHasCryptoBalance, IHasTokenBalance, IAsyncDisposable
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
    private const string GREETING_CONTRACT_BYTECODE = "0x608060405234801561001057600080fd5b50336000806101000a81548173ffffffffffffffffffffffffffffffffffffffff021916908373ffffffffffffffffffffffffffffffffffffffff1602179055506101be806100606000396000f3fe608060405234801561001057600080fd5b5060043610610053576000357c01000000000000000000000000000000000000000000000000000000009004806341c0e1b514610058578063cfae321714610062575b600080fd5b6100606100e5565b005b61006a610155565b6040518080602001828103825283818151815260200191508051906020019080838360005b838110156100aa57808201518184015260208101905061008f565b50505050905090810190601f1680156100d75780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b6000809054906101000a900473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff161415610153573373ffffffffffffffffffffffffffffffffffffffff16ff5b565b60606040805190810160405280600d81526020017f48656c6c6f2c20776f726c64210000000000000000000000000000000000000081525090509056fea165627a7a7230582077fbec49f64eda23cb526275088f65c1fc7e8d002b4681e098f18292791cd94b0029";

    public static async Task<GreetingContract> CreateAsync(Action<GreetingContract>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Greeting Contract Instance");
        var (publicKey, privateKey) = Generator.KeyPair();
        var fileParams = new CreateFileParams
        {
            Expiration = DateTime.UtcNow.AddSeconds(7890000),
            Endorsements = [TestNetwork.Endorsement],
            Contents = Encoding.UTF8.GetBytes(GREETING_CONTRACT_BYTECODE)
        };
        await using var client = await TestNetwork.CreateClientAsync();
        var fileReceipt = await client.CreateFileAsync(fileParams, ctx => ctx.Memo = Generator.Code(20));
        var contractParams = new CreateContractParams
        {
            File = fileReceipt.File,
            Administrator = publicKey,
            Signatory = privateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            RenewPeriod = TimeSpan.FromSeconds(7890000),
            Memo = Generator.Code(50)
        };
        var fixture = new GreetingContract
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
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Greeting Contract Instance Created");
        return fixture;
    }
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    public async Task<ulong> GetCryptoBalanceAsync()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        return await client.GetContractBalanceAsync(ContractReceipt!.Contract);
    }
    public async Task<long?> GetTokenBalanceAsync(EntityId token)
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        return await mirror.GetAccountTokenBalanceAsync(ContractReceipt!.Contract, token);
    }
    public async Task<TokenHoldingData[]> GetTokenBalancesAsync()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var list = new List<TokenHoldingData>();
        await foreach (var record in mirror.GetAccountTokenHoldingsAsync(ContractReceipt!.Contract))
        {
            list.Add(record);
        }
        return list.ToArray();
    }
    public static implicit operator EntityId(GreetingContract fixture)
    {
        return fixture.ContractReceipt.Contract;
    }
    public static implicit operator Signatory(GreetingContract fixture)
    {
        return fixture.PrivateKey;
    }
}
