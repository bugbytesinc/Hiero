using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Mirror;

public class MirrorContractDataTests
{
    [Test]
    public async Task Can_Get_Contract()
    {
        // Although a few existing tests use GetContractAsync as a setup helper,
        // none assert its full payload. This is the explicit integration check.
        await using var fx = await GreetingContract.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetContractAsync(fx.ContractReceipt!.Contract);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.HapiAddress).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(data.Endorsement).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(data.File).IsEqualTo(fx.FileReceipt.File);
        await Assert.That(data.Created.Seconds > 0).IsTrue();
        await Assert.That(data.Deleted).IsFalse();
        await Assert.That(data.AutoRenewPeriod).IsEqualTo((long)fx.ContractParams.RenewPeriod.TotalSeconds);
        await Assert.That(data.EvmAddress).IsNotEqualTo(EvmAddress.None);
        await Assert.That(data.Bytecode.IsEmpty).IsFalse();
        await Assert.That(data.RuntimeBytecode.IsEmpty).IsFalse();
        await Assert.That(data.Nonce >= 0).IsTrue();
    }

    [Test]
    public async Task Can_Get_Contracts_Filtered_By_Id()
    {
        await using var fx = await GreetingContract.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var matches = new List<ContractData>();
        await foreach (var data in mirror.GetContractsAsync(ContractFilter.Is(fx.ContractReceipt!.Contract)))
        {
            matches.Add(data);
            if (matches.Count >= 5)
            {
                break;
            }
        }

        await Assert.That(matches.Count).IsEqualTo(1);
        await Assert.That(matches[0].HapiAddress).IsEqualTo(fx.ContractReceipt!.Contract);
        // The list endpoint omits bytecodes per the SDK's docstring — confirm.
        await Assert.That(matches[0].Bytecode.IsEmpty).IsTrue();
        await Assert.That(matches[0].RuntimeBytecode.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Can_Get_Contract_State()
    {
        // StatefulContract's constructor stores a string in slot 1 — the
        // first packed-string slot, with slot 0 holding the owner address.
        // We assert that slot 1 has been written and is non-zero.
        await using var fx = await StatefulContract.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var slot = await mirror.GetContractStateAsync(fx.ContractReceipt!.Contract, BigInteger.One, []);

        await Assert.That(slot).IsNotNull();
        await Assert.That(slot!.HapiAddress).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(slot.Slot).IsEqualTo(BigInteger.One);
        await Assert.That(slot.Value.IsEmpty).IsFalse();
        await Assert.That(slot.Value.Length).IsEqualTo(32);
    }

    [Test]
    public async Task Can_Get_Contract_Results_For_A_Contract()
    {
        // Make a contract call so there's at least one result on the contract.
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "greet",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        };
        var receipt = await client.CallContractAsync(callParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        // Re-fetch mirror after the mutating call so it catches up.
        mirror = await TestNetwork.GetMirrorRestClientAsync();
        var results = new List<ContractResultData>();
        await foreach (var result in mirror.GetContractResultsAsync(fx.ContractReceipt!.Contract, new PageLimit(10)))
        {
            results.Add(result);
            if (results.Count >= 10)
            {
                break;
            }
        }
        // Two results expected — one for the create, one for the call. Tolerate
        // the call-only path in case the create record is paged out.
        await Assert.That(results.Count >= 1).IsTrue();
        await Assert.That(results.All(r => r.Contract == fx.ContractReceipt!.Contract)).IsTrue();
    }

    [Test]
    public async Task Can_Look_Up_Contract_Result_From_Multiple_Angles()
    {
        // One call → six lookup paths. Order matters: we need the by-id
        // result first to harvest the EVM hash and consensus timestamp.
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "greet",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        };
        var receipt = await client.CallContractAsync(callParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Consensus).IsNotNull();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        // 1. By transaction id. The contract-results pipeline can lag a beat
        // behind the transactions pipeline that GetMirrorRestClientAsync waits
        // on, so retry briefly if the first read returns null.
        ContractResultData? byId = null;
        for (int attempt = 0; attempt < 5 && byId is null; attempt++)
        {
            byId = await mirror.GetContractResultByTransactionIdAsync(record.TransactionId);
            if (byId is null)
            {
                await Task.Delay(2000);
            }
        }
        await Assert.That(byId).IsNotNull();
        await Assert.That(byId!.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(byId.Hash).IsNotEqualTo(EvmHash.None);
        await Assert.That(byId.BlockHash.Length).IsEqualTo(48);
        await Assert.That(byId.TransactionStatus).IsEqualTo(BigInteger.One);
        var evmHash = byId.Hash;
        var blockHash = byId.BlockHash;
        var consensus = record.Consensus!.Value;
        var blockNumber = byId.BlockNumber;

        // 2. By EVM transaction hash.
        var byHash = await mirror.GetContractResultByTransactionHashAsync(evmHash);
        await Assert.That(byHash).IsNotNull();
        await Assert.That(byHash!.Hash).IsEqualTo(evmHash);

        // 3. By contract id + consensus timestamp.
        var byTimestamp = await mirror.GetContractResultByTimestampAsync(fx.ContractReceipt!.Contract, consensus);
        await Assert.That(byTimestamp).IsNotNull();
        await Assert.That(byTimestamp!.Hash).IsEqualTo(evmHash);

        // 4. By block hash + transaction index.
        var byBlockAndPosition = await mirror.GetContractResultByBlockAndPositionAsync(blockHash, (long)byId.TransactionIndex);
        await Assert.That(byBlockAndPosition).IsNotNull();
        await Assert.That(byBlockAndPosition!.Hash).IsEqualTo(evmHash);

        // 5. By block hash (paginated list).
        var byBlock = new List<ContractResultData>();
        await foreach (var item in mirror.GetContractResultsByBlockHashAsync(blockHash))
        {
            byBlock.Add(item);
            if (byBlock.Count >= 50)
            {
                break;
            }
        }
        await Assert.That(byBlock.Any(r => r.Hash == evmHash)).IsTrue();

        // 6. Across all contracts, narrowed to this block's number — small page.
        var allFiltered = new List<ContractResultData>();
        await foreach (var item in mirror.GetAllContractResultsAsync(BlockNumberFilter.Is((ulong)blockNumber), new PageLimit(50)))
        {
            allFiltered.Add(item);
            if (allFiltered.Count >= 50)
            {
                break;
            }
        }
        await Assert.That(allFiltered.Any(r => r.Hash == evmHash)).IsTrue();
    }

    [Test]
    public async Task Can_Get_Contract_Actions_By_Tx_Id_And_Hash()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "greet",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        };
        var receipt = await client.CallContractAsync(callParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        ContractResultData? byId = null;
        for (int attempt = 0; attempt < 5 && byId is null; attempt++)
        {
            byId = await mirror.GetContractResultByTransactionIdAsync(receipt.TransactionId);
            if (byId is null)
            {
                await Task.Delay(2000);
            }
        }
        await Assert.That(byId).IsNotNull();

        var actionsByTxId = new List<ContractActionData>();
        await foreach (var action in mirror.GetContractActionsByTransactionIdAsync(receipt.TransactionId))
        {
            actionsByTxId.Add(action);
            if (actionsByTxId.Count >= 50)
            {
                break;
            }
        }
        await Assert.That(actionsByTxId.Count >= 1).IsTrue();
        // Top-level action is at depth 0 and targets our contract.
        var topLevel = actionsByTxId.OrderBy(a => a.CallDepth).ThenBy(a => a.Index).First();
        await Assert.That(topLevel.CallDepth).IsEqualTo(0);
        await Assert.That(topLevel.Recipient).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(topLevel.Caller).IsEqualTo(TestNetwork.Payer);
        await Assert.That(topLevel.From).IsNotEqualTo(EvmAddress.None);

        var actionsByHash = new List<ContractActionData>();
        await foreach (var action in mirror.GetContractActionsByTransactionHashAsync(byId!.Hash))
        {
            actionsByHash.Add(action);
            if (actionsByHash.Count >= 50)
            {
                break;
            }
        }
        await Assert.That(actionsByHash.Count).IsEqualTo(actionsByTxId.Count);
    }

    [Test]
    public async Task Can_Get_Contract_Opcodes_By_Tx_Id_And_Hash()
    {
        // Opcode tracing re-executes the transaction on the mirror's EVM —
        // user has flagged it as potentially flaky on testnet, so this test
        // tolerates a MirrorException and just notes it in output.
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "greet",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        };
        var receipt = await client.CallContractAsync(callParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        // Same retry pattern as the multi-angle test — the contract-results
        // pipeline lags the transaction pipeline.
        ContractResultData? byId = null;
        for (int attempt = 0; attempt < 5 && byId is null; attempt++)
        {
            byId = await mirror.GetContractResultByTransactionIdAsync(receipt.TransactionId);
            if (byId is null)
            {
                await Task.Delay(2000);
            }
        }
        await Assert.That(byId).IsNotNull();

        // Opcode tracing may either throw or return null on testnet when the
        // mirror's EVM-replay pipeline hasn't materialized our transaction
        // yet; treat both shapes as a known flaky outcome rather than a
        // hard failure (per user guidance).
        try
        {
            var opcodesByTxId = await mirror.GetContractOpcodesByTransactionIdAsync(receipt.TransactionId);
            if (opcodesByTxId is null)
            {
                TestContext.Current?.OutputWriter.WriteLine("Opcode trace by tx id returned null (mirror re-execution likely not yet ready).");
                return;
            }
            await Assert.That(opcodesByTxId.Opcodes).IsNotNull();
            await Assert.That(opcodesByTxId.Opcodes.Length > 0).IsTrue();
            await Assert.That(string.IsNullOrWhiteSpace(opcodesByTxId.Opcodes[0].Op)).IsFalse();

            var opcodesByHash = await mirror.GetContractOpcodesByTransactionHashAsync(byId!.Hash);
            if (opcodesByHash is null)
            {
                TestContext.Current?.OutputWriter.WriteLine("Opcode trace by hash returned null after by-id succeeded — accepting per known flakiness.");
                return;
            }
            await Assert.That(opcodesByHash.Opcodes.Length).IsEqualTo(opcodesByTxId.Opcodes.Length);
        }
        catch (MirrorException ex)
        {
            TestContext.Current?.OutputWriter.WriteLine($"Opcode tracing flaked on testnet (per known flakiness): {ex.Message}");
        }
    }

    [Test]
    public async Task Can_Get_Contract_Log_Events()
    {
        // EventEmittingContract's send_to(address) emits an event when called.
        // We exercise both the per-contract endpoint and the all-contracts
        // endpoint narrowed by the originating transaction hash.
        await using var fx = await EventEmittingContract.CreateAsync();
        await using var fxRecipient = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "send_to",
            MethodArgs = [fxRecipient.CreateReceipt!.Address],
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        ContractResultData? byId = null;
        for (int attempt = 0; attempt < 5 && byId is null; attempt++)
        {
            byId = await mirror.GetContractResultByTransactionIdAsync(receipt.TransactionId);
            if (byId is null)
            {
                await Task.Delay(2000);
            }
        }
        await Assert.That(byId).IsNotNull();
        var consensus = byId!.Consensus;

        // 1. Per-contract logs endpoint.
        var perContract = new List<ExtendedContractLogData>();
        await foreach (var log in mirror.GetContractLogEventsAsync(fx.ContractReceipt!.Contract))
        {
            perContract.Add(log);
            if (perContract.Count >= 10)
            {
                break;
            }
        }
        await Assert.That(perContract.Count >= 1).IsTrue();
        var ourLog = perContract.FirstOrDefault(l => l.TransactionHash == byId.Hash);
        await Assert.That(ourLog).IsNotNull();
        await Assert.That(ourLog!.RootContract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(ourLog.Topics.Length >= 1).IsTrue();

        // 2. All-contracts logs endpoint, narrowed to a tight time window
        //    around our event so we don't paginate testnet history.
        var startWindow = new ConsensusTimeStamp(consensus.Seconds - 5m);
        var endWindow = new ConsensusTimeStamp(consensus.Seconds + 5m);
        var narrowed = new List<ExtendedContractLogData>();
        await foreach (var log in mirror.GetAllContractLogEventsAsync(
            TimestampFilter.OnOrAfter(startWindow),
            TimestampFilter.OnOrBefore(endWindow),
            new PageLimit(50)))
        {
            narrowed.Add(log);
            if (narrowed.Count >= 50)
            {
                break;
            }
        }
        await Assert.That(narrowed.Any(l => l.TransactionHash == byId.Hash)).IsTrue();
    }

    [Test]
    public async Task Can_Call_Evm_Read_Only()
    {
        // CallEvmAsync simulates a call on the mirror without touching state;
        // GreetingContract.greet() returns "Hello, world!" — verify the
        // simulation returns the same bytes.
        await using var fx = await GreetingContract.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var contractData = await mirror.GetContractAsync(fx.ContractReceipt!.Contract);
        await Assert.That(contractData).IsNotNull();

        var encoded = await mirror.CallEvmAsync(new EvmCallData(contractData!.EvmAddress, "greet")
        {
            From = TestNetwork.Payer.CastToEvmAddress(),
            Gas = 200_000
        });

        await Assert.That(encoded).IsNotNull();
        // Decode the ABI-encoded string.
        await Assert.That(encoded.As<string>()).IsEqualTo("Hello, world!");
    }
}
