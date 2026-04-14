using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Util;

public class GetPseudoRandomNumberTests
{
    [Test]
    public async Task Can_Get_Bounded_Number()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var maxValue = Generator.Integer(1, 20);
        var receipt = await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams { MaxValue = maxValue });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        var ranged = record as RangedPseudoRandomNumberRecord;
        await Assert.That(ranged).IsNotNull();
        await Assert.That(maxValue >= ranged!.Number).IsTrue();
    }

    [Test]
    public async Task Can_Get_Unbounded_Number()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams());
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        var bytes = record as BytesPseudoRandomNumberRecord;
        await Assert.That(bytes).IsNotNull();
        await Assert.That(bytes!.Bytes.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Invalid_Bounds_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var maxValue = -Generator.Integer(1, 20);
        var ex = await Assert.That(async () =>
        {
            await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams { MaxValue = maxValue });
        }).ThrowsException();
        var ae = ex as ArgumentOutOfRangeException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("MaxValue");
        await Assert.That(ae.Message).StartsWith("If specified, the maximum random value must be greater than zero.");
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Pseudo_Random_Number()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new PseudoRandomNumberParams
                {
                    MaxValue = Generator.Integer(1, 100),
                },
                Payer = fxPayer,
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
