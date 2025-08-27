namespace Proto;

internal static class ExchangeRateExtensions
{
    internal static Hiero.ExchangeRate ToExchangeRate(this ExchangeRate exchangeRate)
    {
        return new Hiero.ExchangeRate
        {
            HBarEquivalent = exchangeRate.HbarEquiv,
            USDCentEquivalent = exchangeRate.CentEquiv,
            Expiration = exchangeRate.ExpirationTime.ToConsensusTimeStamp()
        };
    }
}