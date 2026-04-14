using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;

namespace Hiero.Test.Helpers;

public static class Generator
{
    private static readonly char[] _sample = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-*&%$#@!".ToCharArray();
    public static int Integer(int minValueInclusive, int maxValueInclusive)
    {
        return Random.Shared.Next(minValueInclusive, maxValueInclusive + 1);
    }
    public static double Double(double minValueInclusive, double maxValueInclusive)
    {
        return (Random.Shared.NextDouble() * (maxValueInclusive - minValueInclusive)) + minValueInclusive;
    }
    public static string Memo(int minLengthInclusive, int maxLengthInclusive = 0)
    {
        var length = maxLengthInclusive > 0 ? Integer(minLengthInclusive, maxLengthInclusive) : minLengthInclusive;
        return Code(length);
    }
    public static string String(int minLengthInclusive, int maxLengthInclusive)
    {
        return Code(Integer(minLengthInclusive, maxLengthInclusive));
    }
    public static string Code(int length)
    {
        var buffer = new char[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = _sample[Random.Shared.Next(0, _sample.Length)];
        }
        return new string(buffer);
    }
    public static string[] ArrayOfStrings(int minCount, int maxCount, int minLength, int maxLength)
    {
        var result = new string[Integer(minCount, maxCount)];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = String(minLength, maxLength);
        }
        return result;
    }
    public static ConsensusTimeStamp TruncatedFutureDate(int minHoursAhead, int maxHoursAhead)
    {
        var date = TimeProvider.System.GetUtcNow().AddHours(Double(minHoursAhead, maxHoursAhead));
        return new ConsensusTimeStamp(new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc));
    }
    public static ConsensusTimeStamp TruncateToSeconds(DateTime date)
    {
        return new ConsensusTimeStamp(new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Utc));
    }
    public static (ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> privateKey) KeyPair()
    {
        return Random.Shared.Next(0, 2) == 1 ? Ed25519KeyPair() : Secp256k1KeyPair();
    }
    public static (ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> privateKey) Ed25519KeyPair()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = generator.GenerateKeyPair();
        var publicKeyDer = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public).GetDerEncoded();
        var privateKeyDer = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private).GetDerEncoded();
        return (publicKeyDer, privateKeyDer);
    }
    private static readonly X9ECParameters curve = SecNamedCurves.GetByName("secp256k1");
    private static readonly ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
    public static (ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> privateKey) Secp256k1KeyPair()
    {
        var generator = new ECKeyPairGenerator();
        var ecGenerationParameters = new ECKeyGenerationParameters(domain, new SecureRandom());
        generator.Init(ecGenerationParameters);
        var keypair = generator.GenerateKeyPair();
        var privateKeyParameters = (ECPrivateKeyParameters)keypair.Private;
        var publicKeyParameters = (ECPublicKeyParameters)keypair.Public;
        var privateKeyDer = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParameters).GetDerEncoded();
        var publicKeyDer = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKeyParameters).GetDerEncoded();
        return (publicKeyDer, privateKeyDer);
    }
    public static ReadOnlyMemory<byte> SHA384Hash()
    {
        return SHA384.HashData(KeyPair().publicKey.Span);
    }
}