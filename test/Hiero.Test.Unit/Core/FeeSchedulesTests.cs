// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Proto;

namespace Hiero.Test.Unit.Core;

public class FeeSchedulesTests
{
    [Test]
    public async Task FromFile_Parses_Current_And_Next_Fee_Schedules()
    {
        var feeSchedule = new TransactionFeeSchedule
        {
            HederaFunctionality = HederaFunctionality.CryptoTransfer
        };
        feeSchedule.Fees.Add(new FeeData());
        feeSchedule.Fees.Add(new FeeData());
        var set = new CurrentAndNextFeeSchedule
        {
            CurrentFeeSchedule = new Proto.FeeSchedule
            {
                ExpiryTime = new TimestampSeconds { Seconds = 1000 }
            },
            NextFeeSchedule = new Proto.FeeSchedule
            {
                ExpiryTime = new TimestampSeconds { Seconds = 2000 }
            }
        };
        set.CurrentFeeSchedule.TransactionFeeSchedule.Add(feeSchedule);
        var bytes = set.ToByteArray();

        var result = FeeSchedulesExtensions.FromFile(bytes);

        await Assert.That(result.Current).IsNotNull();
        await Assert.That(result.Current!.Expires.Seconds).IsEqualTo(1000);
        await Assert.That(result.Current.Data.Count).IsEqualTo(1);
        await Assert.That(result.Current.Data[HederaFunctionality.CryptoTransfer.ToString()].Length).IsEqualTo(2);
        await Assert.That(result.Next).IsNotNull();
        await Assert.That(result.Next!.Expires.Seconds).IsEqualTo(2000);
        await Assert.That(result.Next.Data).IsEmpty();
    }
}
