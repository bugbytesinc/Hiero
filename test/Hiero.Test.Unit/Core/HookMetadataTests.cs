// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8604, CS8625 // Null assignments and dereferences are intentional in these tests
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class HookMetadataTests
{
    [Test]
    public async Task Constructor_Maps_Properties()
    {
        var id = (long)Generator.Integer(1, 1000);
        var contract = new EntityId(0, 0, Generator.Integer(10, 200));
        var metadata = new HookMetadata(id, contract);
        await Assert.That(metadata.Id).IsEqualTo(id);
        await Assert.That(metadata.Contract).IsEqualTo(contract);
    }

    [Test]
    public async Task AdminKey_Defaults_To_Null()
    {
        var contract = new EntityId(0, 0, Generator.Integer(10, 200));
        var metadata = new HookMetadata(1, contract);
        await Assert.That(metadata.AdminKey).IsNull();
    }

    [Test]
    public async Task InitialStorage_Defaults_To_Null()
    {
        var contract = new EntityId(0, 0, Generator.Integer(10, 200));
        var metadata = new HookMetadata(1, contract);
        await Assert.That(metadata.InitialStorage).IsNull();
    }

    [Test]
    public async Task Null_Contract_Throws_ArgumentNullException()
    {
        EntityId contract = null;
        var ex = Assert.Throws<ArgumentNullException>(() => { new HookMetadata(1, contract); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task None_Contract_Throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => { new HookMetadata(1, EntityId.None); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task With_AdminKey_And_Storage_Maps_Correctly()
    {
        var id = (long)Generator.Integer(1, 1000);
        var contract = new EntityId(0, 0, Generator.Integer(10, 200));
        var adminKey = Endorsement.None;
        var storage = new[]
        {
            new HookStorageEntry(new byte[] { 0x01 }, new byte[] { 0x02 })
        };
        var metadata = new HookMetadata(id, contract, adminKey, storage);
        await Assert.That(metadata.AdminKey).IsEqualTo(adminKey);
        await Assert.That(metadata.InitialStorage).IsNotNull();
    }

    [Test]
    public async Task Equivalent_Metadata_Are_Equal()
    {
        var id = (long)Generator.Integer(1, 1000);
        var contract = new EntityId(0, 0, Generator.Integer(10, 200));
        var m1 = new HookMetadata(id, contract);
        var m2 = new HookMetadata(id, contract);
        await Assert.That(m1).IsEqualTo(m2);
        await Assert.That(m1 == m2).IsTrue();
    }
}
