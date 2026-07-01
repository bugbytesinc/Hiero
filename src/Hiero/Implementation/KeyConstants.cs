// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;

namespace Hiero.Implementation;

internal static class KeyConstants
{
    internal static ReadOnlySpan<byte> HederaSecp256k1PublicKeyDerPrefix =>
    [
        0x30, 0x2d, 0x30, 0x07, 0x06, 0x05, 0x2b, 0x81,
        0x04, 0x00, 0x0a, 0x03, 0x22, 0x00
    ];
    internal static ReadOnlySpan<byte> HederaSecp256k1PrivateKeyDerPrefix =>
    [
        0x30, 0x30, 0x02, 0x01, 0x00, 0x30, 0x07, 0x06,
        0x05, 0x2b, 0x81, 0x04, 0x00, 0x0a, 0x04, 0x22,
        0x04, 0x20
    ];
    internal static readonly X9ECParameters EcdsaSecp256k1Curve = SecNamedCurves.GetByName("secp256k1");
    internal static readonly ECDomainParameters EcdsaSecp256k1DomainParams = new(EcdsaSecp256k1Curve.Curve, EcdsaSecp256k1Curve.G, EcdsaSecp256k1Curve.N, EcdsaSecp256k1Curve.H);
}
