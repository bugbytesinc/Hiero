using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.File;

public class GetFileInfoTests
{
    [Test]
    public async Task Can_Get_File_Info()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetFileInfoAsync(test.CreateReceipt!.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Memo).IsEqualTo(test.CreateParams.Memo);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { test.PublicKey });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Get_Immutable_File_Info()
    {
        await using var test = await TestFile.CreateAsync(fx =>
        {
            fx.CreateParams.Endorsements = Array.Empty<Endorsement>();
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetFileInfoAsync(test.CreateReceipt!.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Memo).IsEqualTo(test.CreateParams.Memo);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Get_Empty_File_Info()
    {
        await using var test = await TestFile.CreateAsync(fx =>
        {
            fx.CreateParams.Contents = ReadOnlyMemory<byte>.Empty;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetFileInfoAsync(test.CreateReceipt!.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Memo).IsEqualTo(test.CreateParams.Memo);
        await Assert.That(info.Size).IsEqualTo(0);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { test.PublicKey });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Get_Immutable_Empty_File_Info()
    {
        await using var test = await TestFile.CreateAsync(fx =>
        {
            fx.CreateParams.Endorsements = Array.Empty<Endorsement>();
            fx.CreateParams.Contents = ReadOnlyMemory<byte>.Empty;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetFileInfoAsync(test.CreateReceipt!.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Memo).IsEqualTo(test.CreateParams.Memo);
        await Assert.That(info.Size).IsEqualTo(0);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }
}
