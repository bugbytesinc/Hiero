using Google.Protobuf;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Extensions;

public class GetAddressBookTests
{
    [Test]
    public async Task Can_Retrieve_Address_Book()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var book = await client.GetAddressBookAsync();
        await Assert.That(book).IsNotNull();
        await Assert.That(book.Length > 0).IsTrue();
        await Assert.That(book.ToDictionary(n => n.Id).Count).IsEqualTo(book.Length);
    }

    [Test]
    public async Task Can_Find_Gateway_RSA_Key_In_Address_Book()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var book = await client.GetAddressBookAsync();
        var node = book.FirstOrDefault(n => n.RsaPublicKey is not null);
        await Assert.That(node).IsNotNull();
        await Assert.That(node!.RsaPublicKey).IsNotNull();
        await Assert.That(node.Endpoints.Length > 0).IsTrue();
    }

    [Test]
    public async Task Can_Get_Address_Book_Manually()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var file = await client.GetFileContentAsync(new EntityId(0, 0, 102));
        var book = Proto.NodeAddressBook.Parser.ParseFrom(file.ToArray());
        await Assert.That(book).IsNotNull();
        var json = JsonFormatter.Default.Format(book);
        TestContext.Current?.OutputWriter.WriteLine(json);
    }
}
