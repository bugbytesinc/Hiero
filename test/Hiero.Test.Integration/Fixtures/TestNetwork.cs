using Google.Protobuf;
using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Microsoft.Extensions.Configuration;
using Proto;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Hiero.Test.Integration.Fixtures;

internal class TestNetwork
{
    private static ConsensusClient _rootClient = default!;
    private static MirrorRestClient _mirrorClient = default!;
    private static Uri _mirrorRestUri = default!;
    private static Uri _mirrorGrpcUri = default!;
    private static AccountData _rootPayer = default!;
    private static ReadOnlyMemory<byte> _privateKey;
    private static ConsensusNodeEndpoint? _fixedEndpoint;
    private static Uri? _blockNodeUri;
    private static Uri? _blockNodeStatusUri;
    private static BlockNodeClient? _rootBlockNodeClient;
    private static EntityId? _privilegedPayer;
    private static ReadOnlyMemory<byte> _privilegedPrivateKey;
    // Mirror catch-up state. _latestKnownMutatingTransaction is the most recent
    // transaction (or paid-query payment) a node ACCEPTED at precheck: only those
    // can reach consensus and so ever appear on the mirror. _latestConfirmedOnMirror
    // is the last one the mirror was seen to hold, so back-to-back mirror reads
    // with no transaction in between cost nothing. _awaitingPrecheck carries the id
    // of the request in flight from the sending hook to the response hook; both
    // fire inside one Engine.SubmitMessageAsync call, so the value flows between
    // them per request.
    private static TransactionId _latestKnownMutatingTransaction = TransactionId.None;
    private static TransactionId _latestConfirmedOnMirror = TransactionId.None;
    private static readonly AsyncLocal<TransactionId?> _awaitingPrecheck = new();
    private static BigInteger? _chainId;
    // Hedera testnet EIP-155 chain id; used when neither configuration nor the
    // mirror can supply one (mainnet 295, testnet 296, previewnet 297, Solo 298).
    private const int DefaultChainId = 296;

    public static EntityId Payer => _rootPayer.Account;
    /// <summary>
    /// The configured mirror node REST endpoint [MirrorRestUrl].
    /// </summary>
    public static Uri MirrorRestUri => _mirrorRestUri;
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
        var blockNodeEndpoint = configuration["BlockNodeEndpoint"];
        if (!string.IsNullOrWhiteSpace(blockNodeEndpoint))
        {
            _blockNodeUri = new Uri(blockNodeEndpoint);
        }
        var blockNodeStatusEndpoint = configuration["BlockNodeStatusEndpoint"];
        if (!string.IsNullOrWhiteSpace(blockNodeStatusEndpoint))
        {
            _blockNodeStatusUri = new Uri(blockNodeStatusEndpoint);
        }
        var privilegedPrivateKey = configuration["PrivilegedPayerPrivateKey"];
        if (!string.IsNullOrWhiteSpace(privilegedPrivateKey))
        {
            _privilegedPrivateKey = Convert.FromHexString(privilegedPrivateKey);
            var privilegedAccountId = (configuration["PrivilegedPayerAccountId"] ?? "0.0.2").Split('.');
            _privilegedPayer = new EntityId(long.Parse(privilegedAccountId[0]), long.Parse(privilegedAccountId[1]), long.Parse(privilegedAccountId[2]));
        }
        var configuredChainId = configuration["ChainId"];
        if (!string.IsNullOrWhiteSpace(configuredChainId))
        {
            _chainId = ParseChainId(configuredChainId);
        }
        if (!string.IsNullOrWhiteSpace(consensusEndpoint))
        {
            var parts = consensusNodeId.Split('.');
            _fixedEndpoint = new ConsensusNodeEndpoint(
                new EntityId(long.Parse(parts[0]), long.Parse(parts[1]), long.Parse(parts[2])),
                new Uri(consensusEndpoint));
        }
        _mirrorRestUri = new Uri(mirrorRestUrl);
        _mirrorClient = new MirrorRestClient(new HttpClient() { BaseAddress = _mirrorRestUri });
        _mirrorGrpcUri = new Uri(mirrorGrpcUrl);
        _privateKey = Convert.FromHexString(payerPrivateKey);
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
            // Polls faster than the default early on, but still for the
            // whole 180 s validity: 85 retries at a 50 ms step span 183 s.
            ctx.RetryCount = 85;
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
    /// <summary>
    /// A responsive consensus endpoint of the given transport, chosen at
    /// random; the configured [ConsensusEndpoint] when one is set.
    /// </summary>
    public static Task<ConsensusNodeEndpoint> GetConsensusNodeEndpointAsync(ConsensusNodeTransport transport = ConsensusNodeTransport.All) => PickConsensusNodeAsync(transport);
    public static ConsensusClient CreateClient(ConsensusNodeEndpoint endpoint) => _rootClient.Clone(ctx => ctx.Endpoint = endpoint);
    /// <summary>
    /// True when a privileged (system-account) payer is configured via
    /// [PrivilegedPayerPrivateKey]; tests requiring elevated authority
    /// skip when it is absent.
    /// </summary>
    public static bool HasPrivilegedPayer => !_privilegedPrivateKey.IsEmpty;
    /// <summary>
    /// The configured privileged payer's key material, for use as an
    /// admin key on transactions it authorizes.
    /// </summary>
    public static Signatory PrivilegedSignatory => new(_privilegedPrivateKey);
    /// <summary>
    /// The endorsement matching <see cref="PrivilegedSignatory"/>.
    /// </summary>
    public static Endorsement PrivilegedEndorsement => PrivilegedSignatory.GetEndorsements()[0];
    /// <summary>
    /// A client paying with (and signing as) the configured privileged
    /// system account — required for operations the network restricts to
    /// elevated payers, such as HIP-1137 registered-node creation.
    /// </summary>
    public static async Task<ConsensusClient> CreatePrivilegedClientAsync()
    {
        if (!HasPrivilegedPayer)
        {
            throw new InvalidOperationException("Privileged Payer Private Key [PrivilegedPayerPrivateKey] is missing from configuration.");
        }
        var endpoint = await PickConsensusNodeAsync();
        return _rootClient.Clone(ctx =>
        {
            ctx.Endpoint = endpoint;
            ctx.Payer = _privilegedPayer;
            ctx.Signatory = PrivilegedSignatory;
        });
    }
    public static MirrorGrpcClient CreateMirrorGrpcClient() => new(ctx => ctx.Uri = _mirrorGrpcUri);
    /// <summary>
    /// The optional block node gRPC endpoint [BlockNodeEndpoint]; block node
    /// tests skip when it is not configured.  A node that serves its status
    /// API on a separate port (as mainnet's registered block nodes do) also
    /// needs [BlockNodeStatusEndpoint].
    /// </summary>
    public static Uri? BlockNodeUri => _blockNodeUri;
    /// <summary>
    /// A block node client cloned from a shared root, mirroring how the
    /// consensus client is shared: the root owns the gRPC channel for the
    /// life of the assembly, so per-test clients do not each open (and
    /// tear down) their own connection.  Creating a fresh client per test
    /// pushed the node to ~22 concurrent connections during a full run,
    /// which intermittently exhausted it.
    /// </summary>
    public static BlockNodeClient CreateBlockNodeClient()
    {
        if (_blockNodeUri is null)
        {
            throw new InvalidOperationException("Block Node Endpoint URL [BlockNodeEndpoint] is missing from configuration.");
        }
        _rootBlockNodeClient ??= new BlockNodeClient(ctx =>
        {
            ctx.Uri = _blockNodeUri;
            ctx.StatusUri = _blockNodeStatusUri;
            ctx.OnSendingRequest = OnClientSendingRequest;
        });
        return _rootBlockNodeClient.Clone();
    }
    public static async Task<MirrorRestClient> GetMirrorRestClientAsync()
    {
        await WaitForMirrorConsensusCatchUpAsync();
        return _mirrorClient;
    }
    /// <summary>
    /// The hbar balance, in tinybars, of an account or contract, read from
    /// the mirror node after it has caught up with the latest transaction
    /// this test run submitted — so the result reflects that transaction.
    /// Consensus nodes no longer answer balance queries (public networks
    /// removed CryptoGetAccountBalance in July 2026; nodes reply BUSY).
    /// </summary>
    public static async Task<ulong> GetCryptoBalanceAsync(EntityId account)
    {
        var mirror = await GetMirrorRestClientAsync();
        var balance = await mirror.GetAccountBalanceAsync(account);
        return balance is long tinybars ? (ulong)tinybars : throw new InvalidOperationException($"The mirror node does not know account {account}.");
    }
    /// <summary>
    /// Resolves the network's EIP-155 chain id for building EVM transactions,
    /// resilient across both testnet and a clean Solo instance. A configured
    /// <c>ChainId</c> (user secrets / environment / appsettings) wins; otherwise
    /// it is derived from the mirror node (reliable on any network with EVM
    /// history, e.g. testnet); if the network has never processed an Ethereum
    /// transaction (e.g. a freshly-started Solo, where the value exists nowhere
    /// to derive) it falls back to the testnet default. Set <c>ChainId=298</c>
    /// in configuration when bootstrapping against Solo. The result is cached —
    /// chain id is static for the life of a network.
    /// </summary>
    public static async Task<BigInteger> GetChainIdAsync()
    {
        if (_chainId.HasValue)
        {
            return _chainId.Value;
        }
        try
        {
            _chainId = await _mirrorClient.GetChainIdAsync();
        }
        catch (MirrorException)
        {
            _chainId = DefaultChainId;
        }
        return _chainId.Value;
    }
    private static BigInteger ParseChainId(string value)
    {
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            // Leading zero keeps BigInteger from reading a high bit as a sign.
            return BigInteger.Parse("0" + value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        return BigInteger.Parse(value, CultureInfo.InvariantCulture);
    }
    /// <summary>
    /// Tinybars worth the given number of US cents at the network's current
    /// exchange rate. Network fees are set in USD, so an account funded in
    /// cents to pay a fee stays sufficient as the hbar price moves.
    /// </summary>
    public static async Task<long> TinybarsFromCentsAsync(int cents)
    {
        var rates = (await _mirrorClient.GetExchangeRateAsync())!.CurrentRate;
        return (long)BigInteger.Divide(BigInteger.Multiply(BigInteger.Multiply(cents, rates.HbarEquivalent), 1_00_000_000), rates.CentEquivalent);
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
        if (_rootBlockNodeClient != null)
        {
            await _rootBlockNodeClient.DisposeAsync();
            _rootBlockNodeClient = null;
        }
    }
    private static async Task<ConsensusNodeEndpoint> PickConsensusNodeAsync(ConsensusNodeTransport transport = ConsensusNodeTransport.All)
    {
        if (_fixedEndpoint is not null)
        {
            return _fixedEndpoint;
        }
        try
        {
            // By default, discover both plaintext (50211) and TLS (50212)
            // endpoints so the suite randomly exercises hash-pinned TLS channels
            // as well as plaintext ones. On a network without usable TLS cert
            // hashes (e.g. Solo), the TLS endpoints are excluded and this
            // degrades to plaintext-only automatically.
            var list = (await _mirrorClient.GetActiveConsensusNodesAsync(2000, transport)).Keys.ToArray();
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
        _awaitingPrecheck.Value = MutatingTransactionIdOf(message);
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
        var pending = _awaitingPrecheck.Value;
        if (pending is not null && PrecheckCodeOf(message) == ResponseCodeEnum.Ok)
        {
            _latestKnownMutatingTransaction = pending;
            _awaitingPrecheck.Value = null;
        }
        var writer = TestContext.Current?.OutputWriter;
        if (writer is null)
        {
            return;
        }
        writer.WriteLine($"{DateTime.UtcNow}  RX:({tryNo:00})   {JsonFormatter.Default.Format(message)}");
    }
    /// <summary>
    /// The id of a transaction, or of a paid query's payment, that will change
    /// network state if the node accepts it; null for anything else (free
    /// queries, cost-only asks, receipts).
    /// </summary>
    private static TransactionId? MutatingTransactionIdOf(IMessage message)
    {
        Transaction? transaction = message switch
        {
            Transaction sent => sent,
            Query query when TryGetQueryTransaction(query, out var payment) => payment,
            _ => null
        };
        if (transaction?.SignedTransactionBytes is null || transaction.SignedTransactionBytes.IsEmpty)
        {
            return null;
        }
        var bodyBytes = SignedTransaction.Parser.ParseFrom(transaction.SignedTransactionBytes).BodyBytes;
        return bodyBytes.IsEmpty ? null : TransactionBody.Parser.ParseFrom(bodyBytes).TransactionID.AsTransactionId();
    }
    /// <summary>
    /// The node's precheck verdict carried by a response, or null for the
    /// engine's diagnostic messages, which are not responses.
    /// </summary>
    private static ResponseCodeEnum? PrecheckCodeOf(IMessage message)
    {
        return message switch
        {
            TransactionResponse response => response.NodeTransactionPrecheckCode,
            Response response => response.ResponseHeader?.NodeTransactionPrecheckCode,
            _ => null
        };
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
    /// <summary>
    /// Waits until the mirror node holds the latest transaction a node accepted
    /// from this test run. The mirror ingests in consensus order, so once that
    /// transaction is present, everything before it is too. Asks the mirror
    /// directly: no consensus-node query, so no fee and no new payment
    /// transaction for the next read to wait on.
    /// </summary>
    private static async Task WaitForMirrorConsensusCatchUpAsync()
    {
        var target = _latestKnownMutatingTransaction;
        if (target == TransactionId.None || target == _latestConfirmedOnMirror)
        {
            return;
        }
        TestContext.Current?.OutputWriter.WriteLine($"{DateTime.UtcNow}  WAIT    ⇌ Waiting for the Mirror to hold transaction {target}");
        var polls = 0;
        var httpErrorCount = 0;
        while (true)
        {
            try
            {
                if ((await _mirrorClient.GetTransactionGroupAsync(target)).Length > 0)
                {
                    _latestConfirmedOnMirror = target;
                    return;
                }
            }
            catch (HttpRequestException hre)
            {
                httpErrorCount++;
                TestContext.Current?.OutputWriter.WriteLine($"{DateTime.UtcNow}  WAIT    ⇌ The Mirror node appears to be struggling {hre.Message}");
                if (httpErrorCount >= 100)
                {
                    throw new Exception($"The Mirror node appears to have gone off to lala land, gave up waiting for transaction {target}", hre);
                }
            }
            polls++;
            if (polls > 500)
            {
                throw new Exception($"The Mirror node appears to be too far out of sync, gave up waiting for transaction {target}");
            }
            if (polls % 10 == 0)
            {
                TestContext.Current?.OutputWriter.WriteLine($"{DateTime.UtcNow}  WAIT    ⇌ Still waiting for the Mirror to hold transaction {target}");
            }
            await Task.Delay(700);
        }
    }
}

