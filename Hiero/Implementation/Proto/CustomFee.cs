using Hiero;

namespace Proto;

internal static class CustomFeeExtensions
{
    internal static CustomFee ToCustomFee(this IRoyalty royalty)
    {
        var result = new CustomFee
        {
            FeeCollectorAccountId = new AccountID(royalty.Receiver)
        };
        switch (royalty.RoyaltyType)
        {
            case RoyaltyType.Fixed:
                var fixedRoyalty = royalty as FixedRoyalty ?? throw new ArgumentException("Royalty had type of Fixed but was not a Fixed Royalty", nameof(royalty));
                result.FixedFee = new FixedFee
                {
                    Amount = fixedRoyalty.Amount,
                    DenominatingTokenId = fixedRoyalty.Token != EntityId.None ? new TokenID(fixedRoyalty.Token) : null
                };
                break;
            case RoyaltyType.Nft:
                var nftRoyalty = royalty as NftRoyalty ?? throw new ArgumentException("Royalty had type of Value (Royalty) but was not a Value Royalty", nameof(royalty));
                result.RoyaltyFee = new RoyaltyFee
                {
                    ExchangeValueFraction = new Fraction
                    {
                        Numerator = nftRoyalty.Numerator,
                        Denominator = nftRoyalty.Denominator
                    }
                };
                if (nftRoyalty.FallbackAmount > 0 || !nftRoyalty.FallbackToken.IsNullOrNone())
                {
                    result.RoyaltyFee.FallbackFee = new FixedFee
                    {
                        Amount = nftRoyalty.FallbackAmount,
                        DenominatingTokenId = nftRoyalty.FallbackToken != EntityId.None ? new TokenID(nftRoyalty.FallbackToken) : null
                    };
                }

                break;
            case RoyaltyType.Token:
                var tokenRoyalty = royalty as TokenRoyalty ?? throw new ArgumentException("Royalty had type of Fractional but was not a Fractional Royalty", nameof(royalty));
                result.FractionalFee = new FractionalFee
                {
                    MinimumAmount = tokenRoyalty.Minimum,
                    MaximumAmount = tokenRoyalty.Maximum,
                    FractionalAmount = new Fraction
                    {
                        Numerator = tokenRoyalty.Numerator,
                        Denominator = tokenRoyalty.Denominator
                    },
                    NetOfTransfers = tokenRoyalty.AssessAsSurcharge
                };
                break;
            default:
                throw new ArgumentException("Unrecognized Royalty Type", nameof(royalty));
        }
        return result;
    }
}