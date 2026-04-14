// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8604 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class HookTests
{
    [Test]
    public async Task Hook_Constructor_MapsProperties()
    {
        var owner = new EntityId(0, 0, Generator.Integer(10, 1000));
        var id = (long)Generator.Integer(1, 1000);
        var hook = new Hook(owner, id);
        await Assert.That(hook.Owner).IsEqualTo(owner);
        await Assert.That(hook.Id).IsEqualTo(id);
    }

    [Test]
    public async Task Hook_None_HasSentinelValues()
    {
        var none = Hook.None;
        await Assert.That(none.Owner).IsEqualTo(EntityId.None);
        var expectedId = 0L;
        await Assert.That(none.Id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task Hook_NullOwner_ThrowsArgumentNullException()
    {
        EntityId owner = null;
        var ex = Assert.Throws<ArgumentNullException>(() => { new Hook(owner, 1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Hook_NoneOwner_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => { new Hook(EntityId.None, 1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Hook_NonShardRealmNumOwner_ThrowsArgumentOutOfRangeException()
    {
        // Create an EntityId with an EVM address (not shard.realm.num form)
        var evmAddress = new EvmAddress(new byte[20]);
        var aliasOwner = new EntityId(0, 0, evmAddress);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { new Hook(aliasOwner, 1); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Hook_Equality_EquivalentHooks_AreEqual()
    {
        var hook1 = new Hook(new EntityId(0, 0, 5), 3);
        var hook2 = new Hook(new EntityId(0, 0, 5), 3);
        await Assert.That(hook1).IsEqualTo(hook2);
    }

    [Test]
    public async Task Hook_Equality_DifferentHooks_AreNotEqual()
    {
        var hook1 = new Hook(new EntityId(0, 0, 5), 3);
        var hook2 = new Hook(new EntityId(0, 0, 5), 4);
        await Assert.That(hook1).IsNotEqualTo(hook2);
    }
}

public class HookCallTests
{
    [Test]
    public async Task HookCall_Constructor_MapsAllProperties()
    {
        var hookId = (long)Generator.Integer(1, 1000);
        var data = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02 });
        var gasLimit = (ulong)Generator.Integer(100, 10000);
        var callMode = HookCallMode.PreAndPost;
        var hookCall = new HookCall(hookId, data, gasLimit, callMode);
        await Assert.That(hookCall.HookId).IsEqualTo(hookId);
        await Assert.That(hookCall.Data.ToArray()).IsEquivalentTo(data.ToArray());
        await Assert.That(hookCall.GasLimit).IsEqualTo(gasLimit);
        await Assert.That(hookCall.CallMode).IsEqualTo(callMode);
    }

    [Test]
    public async Task HookCall_DefaultCallMode_IsPreOnly()
    {
        var hookCall = new HookCall(1, ReadOnlyMemory<byte>.Empty, 100);
        await Assert.That(hookCall.CallMode).IsEqualTo(HookCallMode.PreOnly);
    }

    [Test]
    public async Task HookCall_ExplicitCallMode_MapsCorrectly()
    {
        var hookCall = new HookCall(1, ReadOnlyMemory<byte>.Empty, 100, HookCallMode.PreAndPost);
        await Assert.That(hookCall.CallMode).IsEqualTo(HookCallMode.PreAndPost);
    }

    [Test]
    public async Task HookCall_Equality_EquivalentHookCalls_AreEqual()
    {
        var data = new byte[] { 0x01 };
        var hookCall1 = new HookCall(1, new ReadOnlyMemory<byte>(data), 100, HookCallMode.PreOnly);
        var hookCall2 = new HookCall(1, new ReadOnlyMemory<byte>(data), 100, HookCallMode.PreOnly);
        await Assert.That(hookCall1).IsEqualTo(hookCall2);
    }

    [Test]
    public async Task HookCall_Equality_DifferentHookCalls_AreNotEqual()
    {
        var data = new byte[] { 0x01 };
        var hookCall1 = new HookCall(1, new ReadOnlyMemory<byte>(data), 100, HookCallMode.PreOnly);
        var hookCall2 = new HookCall(2, new ReadOnlyMemory<byte>(data), 100, HookCallMode.PreOnly);
        await Assert.That(hookCall1).IsNotEqualTo(hookCall2);
    }
}
