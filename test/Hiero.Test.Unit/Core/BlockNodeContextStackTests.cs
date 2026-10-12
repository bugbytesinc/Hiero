// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Grpc.Net.Client;
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class BlockNodeContextStackTests
{
    [Test]
    public async Task Can_Set_And_Reset_Uri_Property()
    {
        var uri1 = new Uri("http://blocknode1:40840");
        var uri2 = new Uri("http://blocknode2:40840");
        Uri? captured = null;

        await using var client1 = new BlockNodeClient(context =>
        {
            captured = context.Uri;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Uri = uri1);
        client1.Configure(context => captured = context.Uri);
        await Assert.That(captured).IsEqualTo(uri1);

        await using var client2 = client1.Clone(context =>
        {
            captured = context.Uri;
        });
        await Assert.That(captured).IsEqualTo(uri1);

        client2.Configure(context => context.Uri = uri2);
        client2.Configure(context => captured = context.Uri);
        await Assert.That(captured).IsEqualTo(uri2);

        client2.Configure(context => context.Reset(nameof(IBlockNodeContext.Uri)));
        client2.Configure(context => captured = context.Uri);
        await Assert.That(captured).IsEqualTo(uri1);

        client1.Configure(context => context.Reset(nameof(IBlockNodeContext.Uri)));
        client1.Configure(context => captured = context.Uri);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.Uri);
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.Uri = uri2);
        client2.Configure(context => captured = context.Uri);
        await Assert.That(captured).IsEqualTo(uri2);
    }

    [Test]
    public async Task Can_Set_And_Reset_StatusUri_Property()
    {
        var uri = new Uri("http://blocknode1:40840");
        var statusUri1 = new Uri("http://blocknode1:40982");
        var statusUri2 = new Uri("http://blocknode2:40982");
        Uri? captured = null;

        await using var client1 = new BlockNodeClient(context =>
        {
            context.Uri = uri;
            captured = context.StatusUri;
        });
        await Assert.That(captured).IsNull();

        client1.Configure(context => context.StatusUri = statusUri1);
        await using var client2 = client1.Clone(context => captured = context.StatusUri);
        await Assert.That(captured).IsEqualTo(statusUri1);

        client2.Configure(context => context.StatusUri = statusUri2);
        client2.Configure(context => captured = context.StatusUri);
        await Assert.That(captured).IsEqualTo(statusUri2);

        // Setting null in a child means "use Uri" for that child only.
        client2.Configure(context => context.StatusUri = null);
        client2.Configure(context => captured = context.StatusUri);
        await Assert.That(captured).IsNull();
        client1.Configure(context => captured = context.StatusUri);
        await Assert.That(captured).IsEqualTo(statusUri1);

        client2.Configure(context => context.Reset(nameof(IBlockNodeContext.StatusUri)));
        client2.Configure(context => captured = context.StatusUri);
        await Assert.That(captured).IsEqualTo(statusUri1);

        client1.Configure(context => context.Reset(nameof(IBlockNodeContext.StatusUri)));
        client2.Configure(context => captured = context.StatusUri);
        await Assert.That(captured).IsNull();
    }

    [Test]
    public async Task Can_Set_And_Reset_RetryCount_Property()
    {
        var defaultValue = 0;
        var newValue = Generator.Integer(50, 100);
        int captured = 0;

        await using var client = new BlockNodeClient(context =>
        {
            defaultValue = context.RetryCount;
            context.RetryCount = newValue;
        });
        await Assert.That(defaultValue).IsEqualTo(5);

        client.Configure(context => captured = context.RetryCount);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.RetryCount;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IBlockNodeContext.RetryCount)));
        client.Configure(context => captured = context.RetryCount);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.RetryCount);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_RetryDelay_Property()
    {
        var defaultValue = TimeSpan.Zero;
        var newValue = TimeSpan.FromMilliseconds(Generator.Integer(500, 900));
        TimeSpan captured = TimeSpan.Zero;

        await using var client = new BlockNodeClient(context =>
        {
            defaultValue = context.RetryDelay;
            context.RetryDelay = newValue;
        });
        await Assert.That(defaultValue).IsEqualTo(TimeSpan.FromMilliseconds(200));

        client.Configure(context => captured = context.RetryDelay);
        await Assert.That(captured).IsEqualTo(newValue);

        await using var clone = client.Clone(context =>
        {
            captured = context.RetryDelay;
        });
        await Assert.That(captured).IsEqualTo(newValue);

        client.Configure(context => context.Reset(nameof(IBlockNodeContext.RetryDelay)));
        client.Configure(context => captured = context.RetryDelay);
        await Assert.That(captured).IsEqualTo(defaultValue);

        clone.Configure(context => captured = context.RetryDelay);
        await Assert.That(captured).IsEqualTo(defaultValue);
    }

    [Test]
    public async Task Can_Set_And_Reset_OnSendingRequest_Property()
    {
        Action<IMessage> callback1 = _ => { };
        Action<IMessage> callback2 = _ => { };
        Action<IMessage>? captured = null;

        await using var client1 = new BlockNodeClient(context =>
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

        client2.Configure(context => context.Reset(nameof(IBlockNodeContext.OnSendingRequest)));
        client2.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsEqualTo(callback1);

        client1.Configure(context => context.Reset(nameof(IBlockNodeContext.OnSendingRequest)));
        client1.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsNull();

        client2.Configure(context => captured = context.OnSendingRequest);
        await Assert.That(captured).IsNull();
    }

    [Test]
    public async Task Reset_With_Invalid_Name_Throws()
    {
        await using var client = new BlockNodeClient();
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.Configure(context => context.Reset("NotAProperty")));
        await Assert.That(exception.Message).Contains("NotAProperty");
    }

    [Test]
    public async Task Null_Channel_Factory_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new BlockNodeClient((Func<Uri, GrpcChannel>)null!));
        await Assert.That(exception.ParamName).IsEqualTo("channelFactory");
    }

    [Test]
    public async Task Null_Configure_Action_Throws()
    {
        await using var client = new BlockNodeClient();
        var exception = Assert.Throws<ArgumentNullException>(() => client.Configure(null!));
        await Assert.That(exception.ParamName).IsEqualTo("configure");
    }
}
