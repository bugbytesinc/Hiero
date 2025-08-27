using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC.Multiplier;
using Org.BouncyCastle.Utilities;

namespace Hiero.Implementation;

internal class ExtendedEcdsaSigner : ECDsaSigner
{
    public ExtendedEcdsaSigner(IDsaKCalculator kCalculator) : base(kCalculator) { }
    public BigInteger[] GenerateSignatureWithRecoveryId(byte[] message)
    {
        ECDomainParameters parameters = key.Parameters;
        BigInteger n = parameters.N;
        BigInteger bigInteger = CalculateE(n, message);
        BigInteger d = ((ECPrivateKeyParameters)key).D;
        if (kCalculator.IsDeterministic)
        {
            kCalculator.Init(n, d, message);
        }
        else
        {
            kCalculator.Init(n, random);
        }

        ECMultiplier eCMultiplier = CreateBasePointMultiplier();
        BigInteger r;
        BigInteger s;
        Org.BouncyCastle.Math.EC.ECPoint point;
        while (true)
        {
            BigInteger bigInteger2 = kCalculator.NextK();
            point = eCMultiplier.Multiply(parameters.G, bigInteger2).Normalize();
            r = point.AffineXCoord.ToBigInteger().Mod(n);
            if (r.SignValue != 0)
            {
                s = BigIntegers.ModOddInverse(n, bigInteger2).Multiply(bigInteger.Add(d.Multiply(r))).Mod(n);
                if (s.SignValue != 0)
                {
                    break;
                }
            }
        }
        BigInteger v = point.AffineYCoord.TestBitZero() ? BigInteger.One : BigInteger.Zero;
        return [v, r, s];
    }
}
