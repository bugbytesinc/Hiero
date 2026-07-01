// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Proto;

namespace Hiero.Test.Unit.Token;

public class TokenInfoTests
{
    [Test]
    public async Task Constructor_Maps_Custom_Fees_To_Royalty_Array()
    {
        var receiver = new EntityId(0, 0, 1001);
        var denominatingToken = new EntityId(0, 0, 2002);
        var response = CreateTokenInfoResponse(receiver, denominatingToken);

        var tokenInfo = new TokenInfo(response);

        await Assert.That(tokenInfo.Royalties.Count).IsEqualTo(3);
        await Assert.That(tokenInfo.Royalties[0] is FixedRoyalty).IsTrue();
        await Assert.That(tokenInfo.Royalties[1] is TokenRoyalty).IsTrue();
        await Assert.That(tokenInfo.Royalties[2] is NftRoyalty).IsTrue();

        var fixedRoyalty = (FixedRoyalty)tokenInfo.Royalties[0];
        await Assert.That(fixedRoyalty.Receiver).IsEqualTo(receiver);
        await Assert.That(fixedRoyalty.Token).IsEqualTo(denominatingToken);
        await Assert.That(fixedRoyalty.Amount).IsEqualTo(5);

        var tokenRoyalty = (TokenRoyalty)tokenInfo.Royalties[1];
        await Assert.That(tokenRoyalty.Receiver).IsEqualTo(receiver);
        await Assert.That(tokenRoyalty.Numerator).IsEqualTo(1);
        await Assert.That(tokenRoyalty.Denominator).IsEqualTo(10);
        await Assert.That(tokenRoyalty.Minimum).IsEqualTo(2);
        await Assert.That(tokenRoyalty.Maximum).IsEqualTo(50);
        await Assert.That(tokenRoyalty.AssessAsSurcharge).IsTrue();

        var nftRoyalty = (NftRoyalty)tokenInfo.Royalties[2];
        await Assert.That(nftRoyalty.Receiver).IsEqualTo(receiver);
        await Assert.That(nftRoyalty.Numerator).IsEqualTo(3);
        await Assert.That(nftRoyalty.Denominator).IsEqualTo(20);
        await Assert.That(nftRoyalty.FallbackAmount).IsEqualTo(7);
        await Assert.That(nftRoyalty.FallbackToken).IsEqualTo(denominatingToken);
    }

    private static Response CreateTokenInfoResponse(EntityId receiver, EntityId denominatingToken)
    {
        var tokenInfo = new Proto.TokenInfo
        {
            TokenId = new TokenID(new EntityId(0, 0, 2001)),
            TokenType = Proto.TokenType.FungibleCommon,
            Symbol = "TOK",
            Name = "Token",
            Treasury = new AccountID(new EntityId(0, 0, 1000)),
            Expiry = new Timestamp { Seconds = 1000, Nanos = 1 },
            LedgerId = ByteString.CopyFrom([0x00, 0x01])
        };

        tokenInfo.CustomFees.Add(new CustomFee
        {
            FeeCollectorAccountId = new AccountID(receiver),
            FixedFee = new FixedFee
            {
                Amount = 5,
                DenominatingTokenId = new TokenID(denominatingToken)
            }
        });
        tokenInfo.CustomFees.Add(new CustomFee
        {
            FeeCollectorAccountId = new AccountID(receiver),
            FractionalFee = new FractionalFee
            {
                FractionalAmount = new Fraction { Numerator = 1, Denominator = 10 },
                MinimumAmount = 2,
                MaximumAmount = 50,
                NetOfTransfers = true
            }
        });
        tokenInfo.CustomFees.Add(new CustomFee
        {
            FeeCollectorAccountId = new AccountID(receiver),
            RoyaltyFee = new RoyaltyFee
            {
                ExchangeValueFraction = new Fraction { Numerator = 3, Denominator = 20 },
                FallbackFee = new FixedFee
                {
                    Amount = 7,
                    DenominatingTokenId = new TokenID(denominatingToken)
                }
            }
        });

        return new Response
        {
            TokenGetInfo = new TokenGetInfoResponse
            {
                TokenInfo = tokenInfo
            }
        };
    }
}
