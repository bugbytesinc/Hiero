// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

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
}
