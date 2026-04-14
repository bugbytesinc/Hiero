// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class HookStorageEntryTests
{
    [Test]
    public async Task Direct_Slot_Constructor_Maps_Key_And_Value_IndexKey_Is_Null()
    {
        var key = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02 });
        var value = new ReadOnlyMemory<byte>(new byte[] { 0x03, 0x04 });
        var entry = new HookStorageEntry(key, value);
        await Assert.That(entry.Key.ToArray()).IsEquivalentTo(key.ToArray());
        await Assert.That(entry.Value.ToArray()).IsEquivalentTo(value.ToArray());
        await Assert.That(entry.IndexKey).IsNull();
    }

    [Test]
    public async Task Mapping_Constructor_Maps_Key_IndexKey_And_Value()
    {
        var key = new ReadOnlyMemory<byte>(new byte[] { 0x01 });
        var indexKey = new ReadOnlyMemory<byte>(new byte[] { 0x02 });
        var value = new ReadOnlyMemory<byte>(new byte[] { 0x03 });
        var entry = new HookStorageEntry(key, indexKey, value);
        await Assert.That(entry.Key.ToArray()).IsEquivalentTo(key.ToArray());
        await Assert.That(entry.IndexKey).IsNotNull();
        await Assert.That(entry.IndexKey!.Value.ToArray()).IsEquivalentTo(indexKey.ToArray());
        await Assert.That(entry.Value.ToArray()).IsEquivalentTo(value.ToArray());
    }

    [Test]
    public async Task IsPreimage_Defaults_To_False()
    {
        var key = new ReadOnlyMemory<byte>(new byte[] { 0x01 });
        var indexKey = new ReadOnlyMemory<byte>(new byte[] { 0x02 });
        var value = new ReadOnlyMemory<byte>(new byte[] { 0x03 });
        var entry = new HookStorageEntry(key, indexKey, value);
        await Assert.That(entry.IsPreimage).IsFalse();
    }

    [Test]
    public async Task IsPreimage_True_Maps_Correctly()
    {
        var key = new ReadOnlyMemory<byte>(new byte[] { 0x01 });
        var indexKey = new ReadOnlyMemory<byte>(new byte[] { 0x02 });
        var value = new ReadOnlyMemory<byte>(new byte[] { 0x03 });
        var entry = new HookStorageEntry(key, indexKey, value, isPreimage: true);
        await Assert.That(entry.IsPreimage).IsTrue();
    }

    [Test]
    public async Task Equivalent_Entries_Are_Equal()
    {
        var data = new byte[] { 0x01 };
        var entry1 = new HookStorageEntry(new ReadOnlyMemory<byte>(data), new ReadOnlyMemory<byte>(data));
        var entry2 = new HookStorageEntry(new ReadOnlyMemory<byte>(data), new ReadOnlyMemory<byte>(data));
        await Assert.That(entry1).IsEqualTo(entry2);
        await Assert.That(entry1 == entry2).IsTrue();
    }
}
