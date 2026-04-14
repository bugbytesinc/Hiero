using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Extensions;

public class GetFeeScheduleTests
{
    [Test]
    public async Task Can_Retrieve_Network_Fee_Schedule()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var schedule = await client.GetFeeScheduleAsync();
        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule.Current).IsNotNull();
        await Assert.That(schedule.Next).IsNotNull();

        await Assert.That(schedule.Current.Data["CryptoCreate"]).IsNotNull();
        foreach (var feeDetail in schedule.Current.Data["CryptoCreate"])
        {
            var feeDetailData = Proto.FeeData.Parser.ParseJson(feeDetail);
            await Assert.That(feeDetailData).IsNotNull();
            await Assert.That(feeDetailData.Nodedata.Max > 0).IsTrue();
        }
    }
}
