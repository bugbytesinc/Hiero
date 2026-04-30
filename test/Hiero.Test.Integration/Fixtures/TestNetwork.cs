using Google.Protobuf;
using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Microsoft.Extensions.Configuration;
using Proto;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Hiero.Test.Integration.Fixtures;

internal class TestNetwork
{
    private static ConsensusClient _rootClient = default!;
    private static MirrorRestClient _mirrorClient = default!;
    private static Uri _mirrorGrpcUri = default!;
    private static AccountData _rootPayer = default!;
    private static ReadOnlyMemory<byte> _privateKey;
    private static ConsensusNodeEndpoint? _fixedEndpoint;
    private static TransactionId _latestKnownMutatingTransaction = TransactionId.None;
    private static ConsensusTimeStamp _latestKnownMirrorTimestamp = ConsensusTimeStamp.MinValue;

    public static EntityId Payer => _rootPayer.Account;
    public static Endorsement Endorsement => _rootPayer.Endorsement;
    public static ReadOnlyMemory<byte> PrivateKey => _privateKey;

    // Well-known Hedera system admin account numbers.
    // These tests will only pass if the configured Payer private key has admin
    // rights over these accounts; otherwise the network returns NOT_SUPPORTED
    // or AUTHORIZATION_FAILED. Tests using these addresses should be marked [Skip]
    // unless running against a privileged network configuration.
    public static EntityId SystemDeleteAdminAddress => new EntityId(0, 0, 59);
    public static EntityId SystemUndeleteAdminAddress => new EntityId(0, 0, 60);
    public static EntityId SystemFreezeAdminAddress => new EntityId(0, 0, 58);

    [Before(Assembly)]
    public static async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddUserSecrets<TestNetwork>(true)
            .AddEnvironmentVariables()
            .Build();
        var mirrorRestUrl = configuration["MirrorRestUrl"] ?? throw new InvalidOperationException("Mirror REST Endpoint URL [MirrorRestUrl] is missing from configuration.");
        var mirrorGrpcUrl = configuration["MirrorGrpcUrl"] ?? throw new InvalidOperationException("Mirror gRPC Endpoint URL [MirrorGrpcUrl] is missing from configuration.");
        var payerPrivateKey = configuration["PayerPrivateKey"] ?? throw new InvalidOperationException("Payer Private Key [PayerPrivateKey] is missing from configuration.");
        var consensusEndpoint = configuration["ConsensusEndpoint"];
        var consensusNodeId = configuration["ConsensusNodeId"] ?? "0.0.3";
        if (!string.IsNullOrWhiteSpace(consensusEndpoint))
        {
            var parts = consensusNodeId.Split('.');
            _fixedEndpoint = new ConsensusNodeEndpoint(
                new EntityId(long.Parse(parts[0]), long.Parse(parts[1]), long.Parse(parts[2])),
                new Uri(consensusEndpoint));
        }
        _mirrorClient = new MirrorRestClient(new HttpClient() { BaseAddress = new Uri(mirrorRestUrl) });
        _mirrorGrpcUri = new Uri(mirrorGrpcUrl);
        _privateKey = Hex.ToBytes(payerPrivateKey);
        var signatory = new Signatory(_privateKey);
        var endorsement = signatory.GetEndorsements()[0];
        var payerAccountId = configuration["PayerAccountId"];
        if (!string.IsNullOrWhiteSpace(payerAccountId))
        {
            var idParts = payerAccountId.Split('.');
            _rootPayer = new AccountData
            {
                Account = new EntityId(long.Parse(idParts[0]), long.Parse(idParts[1]), long.Parse(idParts[2])),
                Endorsement = endorsement
            };
        }
        else
        {
            _rootPayer = await LookupPayerAsync(endorsement);
        }
        _rootClient = new ConsensusClient(ctx =>
        {
            ctx.Payer = _rootPayer.Account;
            ctx.Signatory = signatory;
            ctx.RetryCount = 50;
            ctx.RetryDelay = TimeSpan.FromMilliseconds(50);
            ctx.OnSendingRequest = OnClientSendingRequest;
            ctx.OnResponseReceived = OnClientResponseReceived;
            ctx.AdjustForLocalClockDrift = true;
            ctx.FeeLimit = 60_00_000_000;
            ctx.QueryTip = 2;
        });
    }
    public static async Task<ConsensusClient> CreateClientAsync()
    {
        var endpoint = await PickConsensusNodeAsync();
        return _rootClient.Clone(ctx => ctx.Endpoint = endpoint);
    }
    public static Task<ConsensusNodeEndpoint> GetConsensusNodeEndpointAsync() => PickConsensusNodeAsync();
    public static ConsensusClient CreateClient(ConsensusNodeEndpoint endpoint) => _rootClient.Clone(ctx => ctx.Endpoint = endpoint);
    public static MirrorGrpcClient CreateMirrorGrpcClient() => new(ctx => ctx.Uri = _mirrorGrpcUri);
    public static async Task<MirrorRestClient> GetMirrorRestClientAsync()
    {
        await WaitForMirrorConsensusCatchUpAsync();
        return _mirrorClient;
    }
    public static async Task<long> EstimateGasFromCentsAsync(int cents)
    {
        var fees = await _mirrorClient.GetNetworkFeesAsync(ConsensusTimeStamp.Now);
        var rates = (await _mirrorClient.GetExchangeRateAsync())!.CurrentRate;
        return (long)BigInteger.Divide(BigInteger.Multiply(BigInteger.Multiply(cents, rates.HbarEquivalent), 1_00_000_000), BigInteger.Multiply(rates.CentEquivalent, fees!.Fees[0].GasPrice));
    }
    [After(Assembly)]
    public static async Task CleanupAsync()
    {
        if (_rootClient != null)
        {
            await _rootClient.DisposeAsync();
            _rootClient = null!;
        }
    }
    private static async Task<ConsensusNodeEndpoint> PickConsensusNodeAsync()
    {
        if (_fixedEndpoint is not null)
        {
            return _fixedEndpoint;
        }
        try
        {
            var list = (await _mirrorClient.GetActiveConsensusNodesAsync(2000)).Keys.ToArray();
            if (list.Length == 0)
            {
                throw new InvalidOperationException("Unable to find a consensus node, no consensus endpoints are responding.");
            }
            return list[Random.Shared.Next(list.Length)];
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to find a consensus node.", ex);
        }
    }
    private static async Task<AccountData> LookupPayerAsync(Endorsement endorsement)
    {
        if (endorsement.Type != KeyType.Ed25519 && endorsement.Type != KeyType.ECDSASecp256K1)
        {
            throw new InvalidOperationException("Invalid private key type [PayerPrivateKey] in configuration.");
        }
        await foreach (var account in _mirrorClient.GetAccountsAsync(AccountPublicKeyFilter.Is(endorsement)))
        {
            return account;
        }
        throw new InvalidOperationException("Unable to find payer account from private key [PayerPrivateKey] in configuration.");
    }
    private static void OnClientSendingRequest(IMessage message)
    {
        var writer = TestContext.Current?.OutputWriter;
        if (writer is null)
        {
            return;
        }
        var timestamp = DateTime.UtcNow;
        if (message is Transaction transaction && transaction.SignedTransactionBytes != null)
        {
            var signedTransaction = SignedTransaction.Parser.ParseFrom(transaction.SignedTransactionBytes);
            var transactionBody = TransactionBody.Parser.ParseFrom(signedTransaction.BodyBytes);
            var transactionId = transactionBody.TransactionID;
            var transactionHeader = transactionBody.Clone();
            transactionHeader.ClearData();
            var transactionContents = transactionBody.Clone();
            transactionContents.TransactionID = null;
            transactionContents.NodeAccountID = null;
            transactionContents.TransactionFee = 0;
            transactionContents.TransactionValidDuration = null;
            transactionContents.Memo = string.Empty;
            writer.WriteLine($"{timestamp}  TX      → {transactionBody.DataCase.ToString().ToUpperInvariant()}: 0.0.{transactionId.AccountID.AccountNum}@{transactionId.TransactionValidStart.Seconds}.{transactionId.TransactionValidStart.Nanos:D9}");
            writer.WriteLine($"{timestamp}  ├─ HEAD   {JsonFormatter.Default.Format(transactionHeader)}");
            writer.WriteLine($"{timestamp}  ├─ SIG    {JsonFormatter.Default.Format(signedTransaction.SigMap)}");
            writer.WriteLine($"{timestamp}  └─ BODY   {JsonFormatter.Default.Format(transactionContents)}");
            _latestKnownMutatingTransaction = transactionId.AsTransactionId();
        }
        else if (message is Query query && TryGetQueryTransaction(query, out Transaction? payment) && payment.SignedTransactionBytes != null)
        {
            var signedTransaction = SignedTransaction.Parser.ParseFrom(payment.SignedTransactionBytes);
            if (signedTransaction.BodyBytes.IsEmpty)
            {
                writer.WriteLine($"{timestamp}  QX ASK  → {JsonFormatter.Default.Format(message)}");
            }
            else
            {
                var transactionBody = TransactionBody.Parser.ParseFrom(signedTransaction.BodyBytes);
                writer.WriteLine($"{timestamp}  QX PYMT → {JsonFormatter.Default.Format(transactionBody)}");
                writer.WriteLine($"{timestamp}  ├─ SIG    {JsonFormatter.Default.Format(signedTransaction.SigMap)}");
                writer.WriteLine($"{timestamp}  └─ QRY    {JsonFormatter.Default.Format(query)}");
                _latestKnownMutatingTransaction = transactionBody.TransactionID.AsTransactionId();
            }
        }
        else if (message is Query queryReceipt && queryReceipt.QueryCase == Query.QueryOneofCase.TransactionGetReceipt)
        {
            if (queryReceipt.TransactionGetReceipt.IncludeChildReceipts)
            {
                var transactionId = queryReceipt.TransactionGetReceipt.TransactionID;
                writer.WriteLine($"{timestamp}  RCPTS   → 0.0.{transactionId.AccountID.AccountNum}@{transactionId.TransactionValidStart.Seconds}.{transactionId.TransactionValidStart.Nanos:D9}");
                writer.WriteLine($"{timestamp}  └─ QRY    {JsonFormatter.Default.Format(message)}");
            }
            else
            {
                writer.WriteLine($"{timestamp}  RECEIPT   {JsonFormatter.Default.Format(message)}");
            }
        }
        else if (message is Com.Hedera.Mirror.Api.Proto.ConsensusTopicQuery)
        {
            writer.WriteLine($"{timestamp}  MR-QRY  → {JsonFormatter.Default.Format(message)}");
        }
        else
        {
            writer.WriteLine($"{timestamp}  TX      → {JsonFormatter.Default.Format(message)}");
        }
    }
    private static void OnClientResponseReceived(int tryNo, IMessage message)
    {
        var writer = TestContext.Current?.OutputWriter;
        if (writer is null)
        {
            return;
        }
        writer.WriteLine($"{DateTime.UtcNow}  RX:({tryNo:00})   {JsonFormatter.Default.Format(message)}");
    }
    private static bool TryGetQueryTransaction(Query query, [NotNullWhen(true)] out Transaction? payment)
    {
        payment = null;
        switch (query.QueryCase)
        {
            case Query.QueryOneofCase.ContractCallLocal:
                payment = query.ContractCallLocal?.Header?.Payment;
                break;
            case Query.QueryOneofCase.ContractGetInfo:
                payment = query.ContractGetInfo?.Header?.Payment;
                break;
            case Query.QueryOneofCase.ContractGetBytecode:
                payment = query.ContractGetBytecode?.Header?.Payment;
                break;
            case Query.QueryOneofCase.ContractGetRecords:
#pragma warning disable CS0612 // Type or member is obsolete
                payment = query.ContractGetRecords?.Header?.Payment;
#pragma warning restore CS0612 // Type or member is obsolete
                break;
            case Query.QueryOneofCase.CryptogetAccountBalance:
                payment = query.CryptogetAccountBalance?.Header?.Payment;
                break;
            case Query.QueryOneofCase.CryptoGetAccountRecords:
                payment = query.CryptoGetAccountRecords?.Header?.Payment;
                break;
            case Query.QueryOneofCase.CryptoGetInfo:
                payment = query.CryptoGetInfo?.Header?.Payment;
                break;
            case Query.QueryOneofCase.FileGetContents:
                payment = query.FileGetContents?.Header?.Payment;
                break;
            case Query.QueryOneofCase.FileGetInfo:
                payment = query.FileGetInfo?.Header?.Payment;
                break;
            case Query.QueryOneofCase.TransactionGetReceipt:
                payment = query.TransactionGetReceipt?.Header?.Payment;
                break;
            case Query.QueryOneofCase.TransactionGetRecord:
                payment = query.TransactionGetRecord?.Header?.Payment;
                break;
            case Query.QueryOneofCase.ScheduleGetInfo:
                payment = query.ScheduleGetInfo?.Header?.Payment;
                break;
        }
        return payment != null;
    }
    private static async Task WaitForMirrorConsensusCatchUpAsync()
    {
        if (_latestKnownMutatingTransaction == TransactionId.None)
        {
            return;
        }
        var client = await CreateClientAsync();
        var lastMutatingConsensus = (await client.GetTransactionRecordAsync(_latestKnownMutatingTransaction)).Consensus;
        if (_latestKnownMirrorTimestamp < lastMutatingConsensus)
        {
            TestContext.Current?.OutputWriter.WriteLine($"{DateTime.UtcNow}  WAIT    ⇌ Waiting for Mirror Consensus Timestamp {lastMutatingConsensus}");
            var count = 0;
            var httpErrorCount = 0;
            do
            {
                try
                {
                    _latestKnownMirrorTimestamp = await _mirrorClient.GetLatestConsensusTimestampAsync();
                    while (_latestKnownMirrorTimestamp < lastMutatingConsensus)
                    {
                        if (count % 10 == 0)
                        {
                            TestContext.Current?.OutputWriter.WriteLine($"{DateTime.UtcNow}  WAIT    ⇌ Waiting for Mirror Consensus Timestamp {lastMutatingConsensus}, last seen Timestamp {_latestKnownMirrorTimestamp}");
                        }
                        if (count > 500)
                        {
                            throw new Exception($"The Mirror node appears to be too far out of sync, gave up waiting for {lastMutatingConsensus}, last seen Timestamp {_latestKnownMirrorTimestamp}");
                        }
                        count++;
                        await Task.Delay(700);
                        _latestKnownMirrorTimestamp = await _mirrorClient.GetLatestConsensusTimestampAsync();
                    }
                    return;
                }
                catch (HttpRequestException hre)
                {
                    httpErrorCount++;
                    TestContext.Current?.OutputWriter.WriteLine($"{DateTime.UtcNow}  WAIT    ⇌ The Mirror node appears to be struggling {hre.Message}");
                }
            } while (httpErrorCount < 1000);
            throw new Exception($"The Mirror node appears to have gone off to lala land, gave up waiting for {lastMutatingConsensus}");
        }
    }
}

