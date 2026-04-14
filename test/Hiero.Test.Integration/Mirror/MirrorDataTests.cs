using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Text;
using System.Text.Json;

namespace Hiero.Test.Integration.Mirror;

public class MirrorDataTests
{
    [Test]
    public async Task Can_Get_Account_Data_From_Ed25519()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Ed25519KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fxAccount);
        var createRecord = await client.GetTransactionRecordAsync(fxAccount.CreateReceipt!.TransactionId);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Account).IsEqualTo(info.Address);
        await Assert.That(data.Alias).IsNull();
        await Assert.That(data.AutoRenewPeriod).IsEqualTo((long)info.AutoRenewPeriod.TotalSeconds);
        await Assert.That((ulong)data.Balances.Balance).IsEqualTo(info.Balance);
        await Assert.That(data.Created == createRecord.Consensus).IsTrue();
        await Assert.That(data.DeclineReward).IsEqualTo(info.StakingInfo.Declined);
        await Assert.That(data.Deleted).IsEqualTo(info.Deleted);
        await Assert.That(data.EvmNonce).IsEqualTo(info.EvmNonce);
        // Note, when created with CreateAccount ...
        // Ed25519 does not have EVM like contract address
        await Assert.That(info.EvmAddress).IsEqualTo(info.Address.CastToEvmAddress());
        // Another network issue, inconsistent precision
        await Assert.That(data.Expiration != info.Expiration).IsTrue();
        await Assert.That((long)data.Expiration.Seconds).IsEqualTo((long)info.Expiration.Seconds);
        // End Bug
        await Assert.That(data.Endorsement).IsEqualTo(info.Endorsement);
        await Assert.That(data.Associations).IsEqualTo(info.AutoAssociationLimit);
        await Assert.That(data.Memo).IsEqualTo(info.Memo);
        await Assert.That(data.PendingReward).IsEqualTo(info.StakingInfo.PendingReward);
        await Assert.That(data.ReceiverSignatureRequired).IsEqualTo(info.ReceiveSignatureRequired);
        await Assert.That(data.StakedAccount).IsNull();
        await Assert.That(data.StakedNode).IsNull();
        await Assert.That(data.StakePeriodStart == ConsensusTimeStamp.MinValue).IsTrue();
    }

    [Test]
    public async Task Can_Get_Account_Data_From_Ecdsa()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            (fx.PublicKey, fx.PrivateKey) = Generator.Secp256k1KeyPair();
            fx.CreateParams.Endorsement = fx.PublicKey;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fxAccount);
        var createRecord = await client.GetTransactionRecordAsync(fxAccount.CreateReceipt!.TransactionId);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(fxAccount);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Account).IsEqualTo(info.Address);
        await Assert.That(data.Alias).IsNull();
        await Assert.That(data.AutoRenewPeriod).IsEqualTo((long)info.AutoRenewPeriod.TotalSeconds);
        await Assert.That((ulong)data.Balances.Balance).IsEqualTo(info.Balance);
        await Assert.That(data.Created == createRecord.Consensus).IsTrue();
        await Assert.That(data.DeclineReward).IsEqualTo(info.StakingInfo.Declined);
        await Assert.That(data.Deleted).IsEqualTo(info.Deleted);
        await Assert.That(data.EvmNonce).IsEqualTo(info.EvmNonce);
        // Note V0.51.0 REGRESSION
        // 20byte EVM address matching public key even on HAPI Create now!
        await Assert.That(info.EvmAddress).IsEqualTo(info.Address.CastToEvmAddress());
        // DEFECT: The mirror node does not get it though!
        await Assert.That(data.EvmAddress).IsEqualTo(fxAccount.CreateReceipt.Address.CastToEvmAddress());
        // Another network issue, inconsistent precision
        await Assert.That(data.Expiration != info.Expiration).IsTrue();
        await Assert.That((long)data.Expiration.Seconds).IsEqualTo((long)info.Expiration.Seconds);
        // End Bug
        await Assert.That(data.Endorsement).IsEqualTo(info.Endorsement);
        await Assert.That(data.Associations).IsEqualTo(info.AutoAssociationLimit);
        await Assert.That(data.Memo).IsEqualTo(info.Memo);
        await Assert.That(data.PendingReward).IsEqualTo(info.StakingInfo.PendingReward);
        await Assert.That(data.ReceiverSignatureRequired).IsEqualTo(info.ReceiveSignatureRequired);
        await Assert.That(data.StakedAccount).IsNull();
        await Assert.That(data.StakedNode).IsNull();
        await Assert.That(data.StakePeriodStart == ConsensusTimeStamp.MinValue).IsTrue();
    }

    [Test]
    public async Task Can_Get_Gossip_Node_List()
    {
        var list = new List<Task<long>>();
        await using var grpClient = await TestNetwork.CreateClientAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        await foreach (var node in mirror.GetConsensusNodesAsync())
        {
            await Assert.That(node.Account.IsNullOrNone()).IsFalse();
            await Assert.That(node.Endpoints).IsNotEmpty();
            await Assert.That(node.Description).IsNotNull();
            await Assert.That(node.File.IsNullOrNone()).IsFalse();
            // 0.53.0 REGRESSION - Why can it be zero now?
            await Assert.That(node.MinimumStake >= 0).IsTrue();
            await Assert.That(node.MaximumStake > 0).IsTrue();
            await Assert.That(node.NodeId >= 0).IsTrue();
            await Assert.That(node.CertificateHash).IsNotEmpty();
            await Assert.That(node.PublicKey).IsNotEmpty();
            await Assert.That(node.RewardRateStart >= 0).IsTrue();
            await Assert.That(node.Stake >= 0).IsTrue();
            await Assert.That(node.StakeNotRewarded >= 0).IsTrue();
            await Assert.That(node.StakeRewarded >= 0).IsTrue();
            await Assert.That(node.ValidRange).IsNotNull();
            await Assert.That(node.ValidRange.Starting).IsNotNull();
            foreach (var endpoint in node.Endpoints)
            {
                await Assert.That(endpoint).IsNotNull();
                await Assert.That(endpoint.Port > 0).IsTrue();
                if (endpoint.Port == 50211 && !string.IsNullOrWhiteSpace(endpoint.Address))
                {
                    list.Add(Task.Run(async () =>
                    {
                        var uri = new Uri($"http://{endpoint.Address}:{endpoint.Port}");
                        var gateway = new ConsensusNodeEndpoint(node.Account, uri);
                        var task = grpClient.PingAsync(default, ctx => ctx.Endpoint = gateway);
                        if (await Task.WhenAny(task, Task.Delay(10000)) == task)
                        {
                            return task.Result;
                        }
                        else
                        {
                            return -1;
                        }
                    }));
                }
            }
        }
        var results = await Task.WhenAll(list);
        TestContext.Current?.OutputWriter.WriteLine(JsonSerializer.Serialize(results));
        await Assert.That(results.Length).IsEqualTo(list.Count);

        var activeGateways = await mirror.GetActiveConsensusNodesAsync(15000);
        // Yes, this is a fuzzy test in that network circumstances
        // could cause this to fail.  But most of the time since the
        // timeout is higher in the second series of pings, this value
        // should almost always be equal to or higher.
        await Assert.That(results.Count(r => r > -1) <= activeGateways.Count).IsTrue();
    }

    [Test]
    public async Task Can_Get_Token_Data()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetTokenAsync(fxToken);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Token).IsEqualTo(info.Token);
        await Assert.That(data.Symbol).IsEqualTo(info.Symbol);
        await Assert.That(data.Name).IsEqualTo(info.Name);
        await Assert.That(data.Memo).IsEqualTo(info.Memo);
        await Assert.That(data.Treasury).IsEqualTo(info.Treasury);
        await Assert.That((uint)data.Decimals).IsEqualTo(info.Decimals);
        await Assert.That((ulong)data.Circulation).IsEqualTo(info.Circulation);
        await Assert.That(data.Ceiling).IsEqualTo(info.Ceiling);
        await Assert.That(data.Type).IsEqualTo(info.Type);
        await Assert.That(data.Modified.Seconds > 0).IsTrue();
        await Assert.That(data.RenewAccount).IsEqualTo(info.RenewAccount);
        await Assert.That(data.RenewPeriodInSeconds).IsEqualTo((long)(info.RenewPeriod?.TotalSeconds ?? 0));
        await Assert.That(data.Administrator).IsEqualTo(info.Administrator);
        await Assert.That(data.SupplyEndorsement).IsEqualTo(info.SupplyEndorsement);
        await Assert.That(data.RoyaltiesEndorsement).IsEqualTo(info.RoyaltiesEndorsement);
        await Assert.That(data.SuspendEndorsement).IsEqualTo(info.SuspendEndorsement);
        await Assert.That(data.GrantKycEndorsement).IsEqualTo(info.GrantKycEndorsement);
        await Assert.That(data.PauseEndorsement).IsEqualTo(info.PauseEndorsement);
        await Assert.That(data.ConfiscateEndorsement).IsEqualTo(info.ConfiscateEndorsement);
        await Assert.That(data.SuspendedByDefault).IsEqualTo(info.TradableStatus == TokenTradableStatus.Suspended);
        await Assert.That(data.PauseStatus).IsEqualTo(info.PauseStatus);
        await Assert.That((ulong)data.InitialSupply).IsEqualTo(info.Circulation);
        await Assert.That(data.SupplyType).IsEqualTo("FINITE");
        await Assert.That(data.Created.Seconds > 0).IsTrue();
        // Another network issue, inconsistent precision
        await Assert.That(data.Expiration != info.Expiration).IsTrue();
        // End Bug
        await Assert.That(data.Deleted).IsEqualTo(info.Deleted);
    }

    [Test]
    public async Task Can_Get_Nft_Data()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetTokenAsync(fxNft);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Token).IsEqualTo(info.Token);
        await Assert.That(data.Symbol).IsEqualTo(info.Symbol);
        await Assert.That(data.Name).IsEqualTo(info.Name);
        await Assert.That(data.Memo).IsEqualTo(info.Memo);
        await Assert.That(data.Treasury).IsEqualTo(info.Treasury);
        await Assert.That((uint)data.Decimals).IsEqualTo(info.Decimals);
        await Assert.That((ulong)data.Circulation).IsEqualTo(info.Circulation);
        await Assert.That(data.Ceiling).IsEqualTo(info.Ceiling);
        await Assert.That(data.Type).IsEqualTo(info.Type);
        await Assert.That(data.Modified.Seconds > 0).IsTrue();
        await Assert.That(data.RenewAccount).IsEqualTo(info.RenewAccount);
        await Assert.That(data.RenewPeriodInSeconds).IsEqualTo((long)(info.RenewPeriod?.TotalSeconds ?? 0));
        await Assert.That(data.Administrator).IsEqualTo(info.Administrator);
        await Assert.That(data.SupplyEndorsement).IsEqualTo(info.SupplyEndorsement);
        await Assert.That(data.RoyaltiesEndorsement).IsEqualTo(info.RoyaltiesEndorsement);
        await Assert.That(data.SuspendEndorsement).IsEqualTo(info.SuspendEndorsement);
        await Assert.That(data.GrantKycEndorsement).IsEqualTo(info.GrantKycEndorsement);
        await Assert.That(data.PauseEndorsement).IsEqualTo(info.PauseEndorsement);
        await Assert.That(data.ConfiscateEndorsement).IsEqualTo(info.ConfiscateEndorsement);
        await Assert.That(data.SuspendedByDefault).IsEqualTo(info.TradableStatus == TokenTradableStatus.Suspended);
        await Assert.That(data.PauseStatus).IsEqualTo(info.PauseStatus);
        await Assert.That(data.InitialSupply).IsEqualTo(0);
        await Assert.That(data.SupplyType).IsEqualTo("FINITE");
        await Assert.That(data.Created.Seconds > 0).IsTrue();
        // Another network issue, inconsistent precision
        await Assert.That(data.Expiration != info.Expiration).IsTrue();
        // End Bug
        await Assert.That(data.Deleted).IsEqualTo(info.Deleted);
    }

    [Test]
    public async Task Can_Get_Hcs_Message()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        for (var i = 0; i < 10; i++)
        {
            var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
            var receipt = await client.SubmitMessageAsync(fxTopic.CreateReceipt!.Topic, message, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxTopic.ParticipantPrivateKey);
            });
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        }
        var expectedMessage = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var submitReceipt = await client.SubmitMessageAsync(fxTopic.CreateReceipt!.Topic, expectedMessage, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxTopic.ParticipantPrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(submitReceipt.TransactionId) as SubmitMessageRecord;
        await Assert.That(record).IsNotNull();

        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetHcsMessageAsync(fxTopic, 11);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ChunkInfo).IsNull();
        await Assert.That(data.TimeStamp == record!.Consensus).IsTrue();
        await Assert.That(data.Message).IsEqualTo(Convert.ToBase64String(expectedMessage));
        await Assert.That(data.Payer).IsEqualTo(record.TransactionId.Payer);
        await Assert.That(data.Hash).IsNotEmpty();
        await Assert.That(data.HashVersion > 0).IsTrue();
        await Assert.That(data.SequenceNumber).IsEqualTo(record.SequenceNumber);
        await Assert.That(data.TopicId).IsEqualTo(fxTopic.CreateReceipt.Topic);
    }

    [Test]
    public async Task Can_Get_All_Hcs_Messages()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var messages = new byte[10][];
        var records = new SubmitMessageRecord[10];
        for (var i = 0; i < messages.Length; i++)
        {
            messages[i] = Encoding.ASCII.GetBytes(Generator.String(10, 100));
            var receipt = await client.SubmitMessageAsync(fxTopic.CreateReceipt!.Topic, messages[i], ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxTopic.ParticipantPrivateKey);
            });
            var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as SubmitMessageRecord;
            await Assert.That(record).IsNotNull();
            records[i] = record!;
        }

        var index = 0;
        await foreach (var data in (await TestNetwork.GetMirrorRestClientAsync()).GetHcsMessagesAsync(fxTopic))
        {
            await Assert.That(data).IsNotNull();
            await Assert.That(data.ChunkInfo).IsNull();
            await Assert.That(data.TimeStamp == records[index].Consensus).IsTrue();
            await Assert.That(data.Message).IsEqualTo(Convert.ToBase64String(messages[index]));
            await Assert.That(data.Payer).IsEqualTo(records[index].TransactionId.Payer);
            await Assert.That(data.Hash).IsNotEmpty();
            await Assert.That(data.HashVersion > 0).IsTrue();
            await Assert.That(data.SequenceNumber).IsEqualTo(records[index].SequenceNumber);
            await Assert.That(data.TopicId).IsEqualTo(fxTopic.CreateReceipt!.Topic);
            index++;
        }
    }

    [Test]
    public async Task Can_Get_Token_Holdings()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var fxTokens = new TestToken[10];
        var xferAmounts = new ulong[fxTokens.Length];
        var assocRecords = new TransactionRecord[fxTokens.Length];
        for (int i = 0; i < xferAmounts.Length; i++)
        {
            fxTokens[i] = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
            var assocReceipt = await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxTokens[i].CreateReceipt!.Token, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
            });
            assocRecords[i] = await client.GetTransactionRecordAsync(assocReceipt.TransactionId);
            xferAmounts[i] = 2 * fxTokens[i].CreateParams.Circulation / 3;
            await client.TransferAsync(new TransferParams
            {
                TokenTransfers = new[]
                {
                    new TokenTransfer(fxTokens[i].CreateReceipt!.Token, fxTokens[i].TreasuryAccount.CreateReceipt!.Address, -(long)xferAmounts[i]),
                    new TokenTransfer(fxTokens[i].CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, (long)xferAmounts[i])
                },
                Signatory = fxTokens[i].TreasuryAccount.PrivateKey
            });
        }
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        await foreach (var data in mirror.GetAccountTokenHoldingsAsync(fxAccount))
        {
            var tokenIndex = FindTokenIndex(data.Token);
            await Assert.That(data).IsNotNull();
            await Assert.That(data.Token).IsEqualTo(fxTokens[tokenIndex].CreateReceipt!.Token);
            await Assert.That(data.AutoAssociated).IsFalse();
            await Assert.That((ulong)data.Balance).IsEqualTo(xferAmounts[tokenIndex]);
            await Assert.That(data.Created == assocRecords[tokenIndex].Consensus).IsTrue();
            await Assert.That(data.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
            await Assert.That(data.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        }

        mirror = await TestNetwork.GetMirrorRestClientAsync();
        for (int i = 0; i < fxTokens.Length; i++)
        {
            var balance = await mirror.GetAccountTokenBalanceAsync(fxAccount, fxTokens[i]);
            await Assert.That((ulong)balance!).IsEqualTo(xferAmounts[i]);
        }

        for (int i = 0; i < fxTokens.Length; i++)
        {
            await fxTokens[i].DisposeAsync();
        }

        int FindTokenIndex(EntityId token)
        {
            for (int i = 0; i < fxTokens.Length; i++)
            {
                if (fxTokens[i].CreateReceipt!.Token == token)
                {
                    return i;
                }
            }
            throw new Exception("token not found");
        }
    }

    [Test]
    public async Task Can_Get_Transfer_Details()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var fxTokens = new TestToken[3];
        var xferAmounts = new ulong[fxTokens.Length];
        var xferRecords = new TransactionRecord[fxTokens.Length];
        for (int i = 0; i < xferAmounts.Length; i++)
        {
            fxTokens[i] = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxTokens[i].CreateReceipt!.Token, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
            });
            xferAmounts[i] = 2 * fxTokens[i].CreateParams.Circulation / 3;
            var xferReceipt = await client.TransferAsync(new TransferParams
            {
                TokenTransfers = new[]
                {
                    new TokenTransfer(fxTokens[i].CreateReceipt!.Token, fxTokens[i].TreasuryAccount.CreateReceipt!.Address, -(long)xferAmounts[i]),
                    new TokenTransfer(fxTokens[i].CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, (long)xferAmounts[i])
                },
                Signatory = fxTokens[i].TreasuryAccount.PrivateKey
            });
            xferRecords[i] = await client.GetTransactionRecordAsync(xferReceipt.TransactionId);
        }
        long feeLimit = 0;
        TimeSpan validDuration = TimeSpan.Zero;
        client.Configure(ctx => { feeLimit = ctx.FeeLimit; validDuration = ctx.TransactionDuration; });
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        for (int i = 0; i < xferAmounts.Length; i++)
        {
            var record = xferRecords[i];
            var dataList = await mirror.GetTransactionGroupAsync(xferRecords[i].TransactionId);
            await Assert.That(dataList).IsNotNull();
            await Assert.That(dataList.Length).IsEqualTo(1);
            var data = dataList[0];
            await Assert.That(data.TransactionId).IsEqualTo(record.TransactionId);
            await Assert.That((ulong)data.Fee).IsEqualTo(record.Fee);
            await Assert.That(data.Consensus == record.Consensus).IsTrue();
            await Assert.That(data.CreatedEntity).IsNull();
            await Assert.That(data.FeeLimit).IsEqualTo(feeLimit);
            await Assert.That(Encoding.UTF8.GetString(data.Memo.Span)).IsEqualTo(record.Memo);
            await Assert.That(data.TransactionType).IsEqualTo("CRYPTOTRANSFER");
            await Assert.That(data.GossipNode).IsNotNull();
            await Assert.That(data.Nonce).IsEqualTo(0);
            await Assert.That(data.ParentConsensus).IsNull();
            await Assert.That(data.Status).IsEqualTo("SUCCESS");
            await Assert.That(data.IsScheduled).IsFalse();
            await Assert.That(data.StakingRewards).IsNotNull();
            await Assert.That(data.Hash.ToArray()).IsEquivalentTo(record.Hash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(data.ValidDuration).IsEqualTo(validDuration);
            await Assert.That(data.ValidStarting == new ConsensusTimeStamp(record.TransactionId.ValidStartSeconds, record.TransactionId.ValidStartNanos)).IsTrue();
            await Assert.That(data.AssessedFees).IsNull();
            await Assert.That(data.AssetTransfers).IsNotNull();
            await Assert.That(data.AssetTransfers!).IsEmpty();
            await Assert.That(data.TokenTransfers).IsNotNull();
            await Assert.That(data.TokenTransfers!.Length).IsEqualTo(2);
            var fromXfer = data.TokenTransfers.First(x => x.Account == fxTokens[i].TreasuryAccount.CreateReceipt!.Address);
            await Assert.That(fromXfer).IsNotNull();
            await Assert.That(fromXfer.Token).IsEqualTo(fxTokens[i].CreateReceipt!.Token);
            await Assert.That(fromXfer.Amount).IsEqualTo(-(long)xferAmounts[i]);
            await Assert.That(fromXfer.IsAllowance).IsFalse();
            var toXfer = data.TokenTransfers.First(x => x.Account == fxAccount.CreateReceipt!.Address);
            await Assert.That(toXfer).IsNotNull();
            await Assert.That(toXfer.Token).IsEqualTo(fxTokens[i].CreateReceipt!.Token);
            await Assert.That(toXfer.Amount).IsEqualTo((long)xferAmounts[i]);
            await Assert.That(toXfer.IsAllowance).IsFalse();
            await Assert.That(data.CryptoTransfers).IsNotNull();
            await Assert.That(data.CryptoTransfers!.Length >= 2).IsTrue();
            await Assert.That(data.CryptoTransfers!.Length <= 5).IsTrue();
        }

        for (int i = 0; i < fxTokens.Length; i++)
        {
            await fxTokens[i].DisposeAsync();
        }
    }

    [Test]
    public async Task Can_Get_Crypto_Allowance_Data()
    {
        await using var fxOwner = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = (long)fxOwner.CreateParams.InitialBalance;
        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxOwner, fxAgent, amount) },
            Signatory = fxOwner
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var list = new List<CryptoAllowanceData>();
        await foreach (var record in (await TestNetwork.GetMirrorRestClientAsync()).GetAccountCryptoAllowancesAsync(fxOwner))
        {
            list.Add(record);
        }
        await Assert.That(list.Count).IsEqualTo(1);

        var info = list.First();
        await Assert.That(info.Amount).IsEqualTo(amount);
        await Assert.That(info.Owner).IsEqualTo(fxOwner.CreateReceipt!.Address);
        await Assert.That(info.Spender).IsEqualTo(fxAgent.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Get_Token_Allowance_Data()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = (long)fxToken.CreateParams.Circulation / 3 + 1;
        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] { new TokenAllowance(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount, fxAgent, amount) },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var mirrorClient = await TestNetwork.GetMirrorRestClientAsync();
        var list = new List<TokenAllowanceData>();
        await foreach (var record in mirrorClient.GetAccountTokenAllowancesAsync(fxToken.TreasuryAccount))
        {
            list.Add(record);
        }
        await Assert.That(list.Count).IsEqualTo(1);

        var info = list.First();
        await Assert.That(info.Amount).IsEqualTo(amount);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Owner).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Spender).IsEqualTo(fxAgent.CreateReceipt!.Address);
    }
}
