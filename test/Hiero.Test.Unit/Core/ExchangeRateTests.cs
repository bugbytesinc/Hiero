// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.Core;

public class ExchangeRateTests
{
    [Test]
    public async Task Equivalent_ExchangeRates_Are_Considered_Equal()
    {
        var hBarEquivalent = Generator.Integer(0, 200);
        var usdCentEquivalent = Generator.Integer(0, 200);
        var expiration = Generator.TruncatedFutureDate(1, 500);
        var rate1 = new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = expiration };
        var rate2 = new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = expiration };
        await Assert.That(rate1).IsEqualTo(rate2);
        await Assert.That(rate1 == rate2).IsTrue();
        await Assert.That(rate1 != rate2).IsFalse();
    }

    [Test]
    public async Task Disimilar_ExchangeRates_Are_Not_Considered_Equal()
    {
        var hBarEquivalent = Generator.Integer(0, 200);
        var usdCentEquivalent = Generator.Integer(0, 200);
        var expiration = Generator.TruncatedFutureDate(1, 500);
        var rate1 = new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = expiration };
        await Assert.That(rate1).IsNotEqualTo(new ExchangeRate { HBarEquivalent = hBarEquivalent + 1, USDCentEquivalent = usdCentEquivalent, Expiration = expiration });
        await Assert.That(rate1).IsNotEqualTo(new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent + 1, Expiration = expiration });
        await Assert.That(rate1).IsNotEqualTo(new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = new ConsensusTimeStamp(expiration.Seconds + 600) });
        await Assert.That(rate1 == new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = new ConsensusTimeStamp(expiration.Seconds + 600) }).IsFalse();
        await Assert.That(rate1 != new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = new ConsensusTimeStamp(expiration.Seconds + 600) }).IsTrue();
    }

    [Test]
    public async Task Equal_ExchangeRates_Have_Equal_HashCodes()
    {
        var hBarEquivalent = Generator.Integer(0, 200);
        var usdCentEquivalent = Generator.Integer(0, 200);
        var expiration = Generator.TruncatedFutureDate(1, 500);
        var rate1 = new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = expiration };
        var rate2 = new ExchangeRate { HBarEquivalent = hBarEquivalent, USDCentEquivalent = usdCentEquivalent, Expiration = expiration };
        await Assert.That(rate1.GetHashCode()).IsEqualTo(rate2.GetHashCode());
    }

    [Test]
    public async Task Disimilar_ExchangeRates_Have_Different_HashCodes()
    {
        var rate1 = new ExchangeRate { HBarEquivalent = 100, USDCentEquivalent = 200, Expiration = Generator.TruncatedFutureDate(1, 500) };
        var rate2 = new ExchangeRate { HBarEquivalent = 101, USDCentEquivalent = 200, Expiration = rate1.Expiration };
        await Assert.That(rate1.GetHashCode()).IsNotEqualTo(rate2.GetHashCode());
    }

    [Test]
    public async Task Default_ExchangeRate_Has_Zero_Values()
    {
        var rate = new ExchangeRate();
        await Assert.That(rate.HBarEquivalent).IsEqualTo(0);
        await Assert.That(rate.USDCentEquivalent).IsEqualTo(0);
    }

    [Test]
    public async Task ToString_Contains_Property_Values()
    {
        var rate = new ExchangeRate { HBarEquivalent = 42, USDCentEquivalent = 99, Expiration = new ConsensusTimeStamp(1000, 0) };
        var result = rate.ToString();
        await Assert.That(result).Contains("42");
        await Assert.That(result).Contains("99");
    }

    [Test]
    public async Task ExchangeRate_Equals_Object_Of_Different_Type_Returns_False()
    {
        var rate = new ExchangeRate { HBarEquivalent = 1, USDCentEquivalent = 2, Expiration = Generator.TruncatedFutureDate(1, 500) };
        await Assert.That(rate.Equals("not an exchange rate")).IsFalse();
    }

    [Test]
    public async Task ExchangeRate_Equals_Null_Returns_False()
    {
        var rate = new ExchangeRate { HBarEquivalent = 1, USDCentEquivalent = 2, Expiration = Generator.TruncatedFutureDate(1, 500) };
        await Assert.That(rate.Equals(null)).IsFalse();
    }

    [Test]
    public async Task FromFile_Parses_Exchange_Rate_Set()
    {
        var set = new ExchangeRateSet
        {
            CurrentRate = new Proto.ExchangeRate
            {
                HbarEquiv = 1,
                CentEquiv = 12,
                ExpirationTime = new TimestampSeconds { Seconds = 1000 }
            },
            NextRate = new Proto.ExchangeRate
            {
                HbarEquiv = 2,
                CentEquiv = 24,
                ExpirationTime = new TimestampSeconds { Seconds = 2000 }
            }
        };
        var bytes = set.ToByteArray();

        var result = ExchangeRatesExtensions.FromFile(bytes);

        await Assert.That(result.Current).IsNotNull();
        await Assert.That(result.Current!.HBarEquivalent).IsEqualTo(1);
        await Assert.That(result.Current.USDCentEquivalent).IsEqualTo(12);
        await Assert.That(result.Current.Expiration.Seconds).IsEqualTo(1000);
        await Assert.That(result.Next).IsNotNull();
        await Assert.That(result.Next!.HBarEquivalent).IsEqualTo(2);
        await Assert.That(result.Next.USDCentEquivalent).IsEqualTo(24);
        await Assert.That(result.Next.Expiration.Seconds).IsEqualTo(2000);
    }
}
