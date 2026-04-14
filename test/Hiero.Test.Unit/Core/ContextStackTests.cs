
// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class ContextStackTests
{
    [Test]
    public async Task Can_Set_And_Reset_Endpoint_Property()
    {
        var gateway1 = new ConsensusNodeEndpoint(new EntityId(0, 0, Generator.Integer(3, 50)), new Uri("http://Gateway1"));
        var gateway2 = new ConsensusNodeEndpoint(new EntityId(0, 0, Generator.Integer(51, 100)), new Uri("http://Gateway2"));
        ConsensusNodeEndpoint? captured = null;

        await using var client1 = new ConsensusClient(context =>
        {
            captured = context.Endpoint;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Endpoint = gateway1);
        client1.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsEqualTo(gateway1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.Endpoint;
        });
        await Assert.That(captured).IsEqualTo(gateway1);

        client2.Configure(context => context.Endpoint = gateway2);
        client2.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsEqualTo(gateway2);

        client2.Configure(context => context.Reset(nameof(IConsensusContext.Endpoint)));
        client2.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsEqualTo(gateway1);

        client1.Configure(context => context.Reset(nameof(IConsensusContext.Endpoint)));
        client1.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Endpoint = gateway2);
        client1.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsEqualTo(gateway2);

        client2.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsEqualTo(gateway2);

        client2.Configure(context => context.Endpoint = null);
        client2.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsNull();

        client1.Configure(context => captured = context.Endpoint);
        await Assert.That(captured).IsEqualTo(gateway2);
    }

    [Test]
    public async Task Can_Set_And_Reset_Payer_Property()
    {
        var account1 = new EntityId(0, 0, Generator.Integer(3, 100));
        var account2 = new EntityId(0, 0, Generator.Integer(101, 200));
        EntityId? captured = null;

        await using var client1 = new ConsensusClient(context =>
        {
            captured = context.Payer;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Payer = account1);
        client1.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsEqualTo(account1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.Payer;
        });
        await Assert.That(captured).IsEqualTo(account1);

        client2.Configure(context => context.Payer = account2);
        client2.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsEqualTo(account2);

        client2.Configure(context => context.Reset(nameof(IConsensusContext.Payer)));
        client2.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsEqualTo(account1);

        client1.Configure(context => context.Reset(nameof(IConsensusContext.Payer)));
        client1.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Payer = account2);
        client1.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsEqualTo(account2);

        client2.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsEqualTo(account2);

        client2.Configure(context => context.Payer = null);
        client2.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsNull();

        client1.Configure(context => captured = context.Payer);
        await Assert.That(captured).IsEqualTo(account2);
    }

    [Test]
    public async Task Can_Set_And_Reset_Signatory_Property()
    {
        var signatory1 = new Signatory(Generator.KeyPair().privateKey);
        var signatory2 = new Signatory(Generator.KeyPair().privateKey);
        Signatory? captured = null;

        await using var client1 = new ConsensusClient(context =>
        {
            captured = context.Signatory;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Signatory = signatory1);
        client1.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsEqualTo(signatory1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.Signatory;
        });
        await Assert.That(captured).IsEqualTo(signatory1);

        client2.Configure(context => context.Signatory = signatory2);
        client2.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsEqualTo(signatory2);

        client2.Configure(context => context.Reset(nameof(IConsensusContext.Signatory)));
        client2.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsEqualTo(signatory1);

        client1.Configure(context => context.Reset(nameof(IConsensusContext.Signatory)));
        client1.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Signatory = signatory2);
        client1.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsEqualTo(signatory2);

        client2.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsEqualTo(signatory2);

        client2.Configure(context => context.Signatory = null);
        client2.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsNull();

        client1.Configure(context => captured = context.Signatory);
        await Assert.That(captured).IsEqualTo(signatory2);
    }

    [Test]
    public async Task Can_Set_And_Reset_FeeLimit_Property()
    {
        var defaultValue = 0L;
        var newValue = (long)Generator.Integer(3, 100);
        long captured = 0;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.FeeLimit;
            context.FeeLimit = newValue;
        });
        client.Configure(context => captured = context.FeeLimit);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.FeeLimit;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.FeeLimit)));
        client.Configure(context => captured = context.FeeLimit);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.FeeLimit);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_TransactionDuration_Property()
    {
        var defaultValue = TimeSpan.Zero;
        var newValue = TimeSpan.FromSeconds(Generator.Integer(200, 300));
        TimeSpan captured = TimeSpan.Zero;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.TransactionDuration;
            context.TransactionDuration = newValue;
        });
        client.Configure(context => captured = context.TransactionDuration);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.TransactionDuration;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.TransactionDuration)));
        client.Configure(context => captured = context.TransactionDuration);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.TransactionDuration);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_Memo_Property()
    {
        var newValue = Generator.String(10, 50);
        string? captured = null;

        await using var client = new ConsensusClient(context =>
        {
            captured = context.Memo;
        });
        await Assert.That(captured).IsNull();

        client.Configure(context => context.Memo = newValue);
        client.Configure(context => captured = context.Memo);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.Memo;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.Memo)));
        client.Configure(context => captured = context.Memo);
        await Assert.That(captured).IsNull();

        clone.Configure(context => captured = context.Memo);
        await Assert.That(captured).IsNull();
    }

    [Test]
    public async Task Can_Set_And_Reset_RetryCount_Property()
    {
        var defaultValue = 0;
        var newValue = Generator.Integer(5000, 6000);
        int captured = 0;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.RetryCount;
            context.RetryCount = newValue;
        });
        client.Configure(context => captured = context.RetryCount);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.RetryCount;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.RetryCount)));
        client.Configure(context => captured = context.RetryCount);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.RetryCount);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_RetryDelay_Property()
    {
        var defaultValue = TimeSpan.Zero;
        var newValue = TimeSpan.FromMinutes(Generator.Integer(200, 300));
        TimeSpan captured = TimeSpan.Zero;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.RetryDelay;
            context.RetryDelay = newValue;
        });
        client.Configure(context => captured = context.RetryDelay);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.RetryDelay;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.RetryDelay)));
        client.Configure(context => captured = context.RetryDelay);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.RetryDelay);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_AdjustForLocalClockDrift_Property()
    {
        var defaultValue = false;
        bool captured = false;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.AdjustForLocalClockDrift;
            context.AdjustForLocalClockDrift = true;
        });
        client.Configure(context => captured = context.AdjustForLocalClockDrift);
        await Assert.That(captured).IsTrue();

        await using var clone = client.Clone(context =>
        {
            captured = context.AdjustForLocalClockDrift;
        });
        await Assert.That(captured).IsTrue();

        client.Configure(context => context.Reset(nameof(IConsensusContext.AdjustForLocalClockDrift)));
        client.Configure(context => captured = context.AdjustForLocalClockDrift);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.AdjustForLocalClockDrift);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_ThrowIfNotSuccess_Property()
    {
        var defaultValue = false;
        bool captured = false;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.ThrowIfNotSuccess;
            context.ThrowIfNotSuccess = false;
        });
        client.Configure(context => captured = context.ThrowIfNotSuccess);
        await Assert.That(captured).IsFalse();

        client.Configure(context => context.ThrowIfNotSuccess = true);
        client.Configure(context => captured = context.ThrowIfNotSuccess);
        await Assert.That(captured).IsTrue();

        await using var clone = client.Clone(context =>
        {
            captured = context.ThrowIfNotSuccess;
        });
        await Assert.That(captured).IsTrue();

        client.Configure(context => context.Reset(nameof(IConsensusContext.ThrowIfNotSuccess)));
        client.Configure(context => captured = context.ThrowIfNotSuccess);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.ThrowIfNotSuccess);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_QueryTip_Property()
    {
        var defaultValue = 0L;
        var newValue = (long)Generator.Integer(100, 500);
        long captured = 0;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.QueryTip;
            context.QueryTip = newValue;
        });
        client.Configure(context => captured = context.QueryTip);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.QueryTip;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.QueryTip)));
        client.Configure(context => captured = context.QueryTip);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.QueryTip);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_SignaturePrefixTrimLimit_Property()
    {
        var defaultValue = 0;
        var newValue = Generator.Integer(1, 100);
        int captured = 0;

        await using var client = new ConsensusClient(context =>
        {
            defaultValue = context.SignaturePrefixTrimLimit;
            context.SignaturePrefixTrimLimit = newValue;
        });
        client.Configure(context => captured = context.SignaturePrefixTrimLimit);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.SignaturePrefixTrimLimit;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IConsensusContext.SignaturePrefixTrimLimit)));
        client.Configure(context => captured = context.SignaturePrefixTrimLimit);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.SignaturePrefixTrimLimit);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_TransactionId_Property()
    {
        var payer = new EntityId(0, 0, Generator.Integer(3, 100));
        var txId1 = new TransactionId(payer, DateTime.UtcNow);
        var txId2 = new TransactionId(payer, DateTime.UtcNow.AddSeconds(1));
        TransactionId? captured = null;

        await using var client1 = new ConsensusClient(context =>
        {
            captured = context.TransactionId;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.TransactionId = txId1);
        client1.Configure(context => captured = context.TransactionId);
        await Assert.That(captured).IsEqualTo(txId1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.TransactionId;
        });
        await Assert.That(captured).IsEqualTo(txId1);

        client2.Configure(context => context.TransactionId = txId2);
        client2.Configure(context => captured = context.TransactionId);
        await Assert.That(captured).IsEqualTo(txId2);

        client2.Configure(context => context.Reset(nameof(IConsensusContext.TransactionId)));
        client2.Configure(context => captured = context.TransactionId);
        await Assert.That(captured).IsEqualTo(txId1);

        client1.Configure(context => context.Reset(nameof(IConsensusContext.TransactionId)));
        client1.Configure(context => captured = context.TransactionId);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.TransactionId);
        await Assert.That(captured).IsNull();
    }

    [Test]
    public async Task Can_Set_And_Reset_OnSendingRequest_Property()
    {
        Action<IMessage> callback1 = _ => { };
        Action<IMessage> callback2 = _ => { };
        Action<IMessage>? captured = null;

        await using var client1 = new ConsensusClient(context =>
        {
            captured = context.OnSendingRequest;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.OnSendingRequest = callback1);
        client1.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsEqualTo(callback1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.OnSendingRequest;
        });
        await Assert.That(captured).IsEqualTo(callback1);

        client2.Configure(context => context.OnSendingRequest = callback2);
        client2.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsEqualTo(callback2);

        client2.Configure(context => context.Reset(nameof(IConsensusContext.OnSendingRequest)));
        client2.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsEqualTo(callback1);

        client1.Configure(context => context.Reset(nameof(IConsensusContext.OnSendingRequest)));
        client1.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsNull();
    }

    [Test]
    public async Task Can_Set_And_Reset_OnResponseReceived_Property()
    {
        Action<int, IMessage> callback1 = (_, _) => { };
        Action<int, IMessage> callback2 = (_, _) => { };
        Action<int, IMessage>? captured = null;

        await using var client1 = new ConsensusClient(context =>
        {
            captured = context.OnResponseReceived;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.OnResponseReceived = callback1);
        client1.Configure(context => captured = context.OnResponseReceived);
        await Assert.That(captured).IsEqualTo(callback1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.OnResponseReceived;
        });
        await Assert.That(captured).IsEqualTo(callback1);

        client2.Configure(context => context.OnResponseReceived = callback2);
        client2.Configure(context => captured = context.OnResponseReceived);
        await Assert.That(captured).IsEqualTo(callback2);

        client2.Configure(context => context.Reset(nameof(IConsensusContext.OnResponseReceived)));
        client2.Configure(context => captured = context.OnResponseReceived);
        await Assert.That(captured).IsEqualTo(callback1);

        client1.Configure(context => context.Reset(nameof(IConsensusContext.OnResponseReceived)));
        client1.Configure(context => captured = context.OnResponseReceived);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.OnResponseReceived);
        await Assert.That(captured).IsNull();
    }
}
