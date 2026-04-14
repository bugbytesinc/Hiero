// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602 // Null assignments and dereferences are intentional in these tests
using Google.Protobuf;
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.Nft;

public class NftInfoTests
{
    [Test]
    public async Task Equivalent_NftInfos_Are_Considered_Equal()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsTrue();
        await Assert.That(nftInfo1 != nftInfo2).IsFalse();
        await Assert.That(nftInfo1.GetHashCode()).IsEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Disimilar_NftInfos_Are_Not_Considered_Equal()
    {
        var nftInfo1 = new NftInfo(GenerateRandomNftInfoResponse());
        var nftInfo2 = new NftInfo(GenerateRandomNftInfoResponse());
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Serial_Numbers_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.NftID.SerialNumber += 1;
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Owners_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.AccountID.AccountNum += 1;
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Created_Times_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.CreationTime.Seconds += 1;
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Metadata_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.Metadata = ByteString.CopyFrom(Generator.SHA384Hash().Span);
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Token_Ids_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.NftID.TokenID.TokenNum += 1;
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Spenders_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.SpenderId.AccountNum += 1;
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Different_Ledgers_Result_In_Not_Equal_NftInfos()
    {
        var response = GenerateRandomNftInfoResponse();
        var nftInfo1 = new NftInfo(response);
        response.TokenGetNftInfo.Nft.LedgerId = ByteString.CopyFrom([(byte)Generator.Integer(1, 255)]);
        var nftInfo2 = new NftInfo(response);
        await Assert.That(nftInfo1).IsNotEqualTo(nftInfo2);
        await Assert.That(nftInfo1 == nftInfo2).IsFalse();
        await Assert.That(nftInfo1 != nftInfo2).IsTrue();
        await Assert.That(nftInfo1.GetHashCode()).IsNotEqualTo(nftInfo2.GetHashCode());
    }

    [Test]
    public async Task Null_NftInfo_Is_Not_Considered_Equal()
    {
        var nftInfo = new NftInfo(GenerateRandomNftInfoResponse());
        await Assert.That(nftInfo.Equals(null as NftInfo)).IsFalse();
    }

    [Test]
    public async Task Properties_Are_Mapped_From_Proto_Response()
    {
        var response = GenerateRandomNftInfoResponse();
        var proto = response.TokenGetNftInfo.Nft;
        var nftInfo = new NftInfo(response);

        await Assert.That(nftInfo.Nft.Token.AccountNum).IsEqualTo(proto.NftID.TokenID.TokenNum);
        await Assert.That(nftInfo.Nft.SerialNumber).IsEqualTo(proto.NftID.SerialNumber);
        await Assert.That(nftInfo.Owner.AccountNum).IsEqualTo(proto.AccountID.AccountNum);
        await Assert.That(nftInfo.Spender.AccountNum).IsEqualTo(proto.SpenderId.AccountNum);
        await Assert.That(nftInfo.Metadata.Span.SequenceEqual(proto.Metadata.Span)).IsTrue();
    }

    private static Response GenerateRandomNftInfoResponse()
    {
        var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(false);
        var nftInfo = new TokenNftInfo
        {
            NftID = new NftID { TokenID = new TokenID { TokenNum = Generator.Integer(100, 200) }, SerialNumber = Generator.Integer(100, 200) },
            AccountID = new AccountID { AccountNum = Generator.Integer(100, 200) },
            SpenderId = new AccountID { AccountNum = Generator.Integer(100, 200) },
            CreationTime = new Timestamp { Seconds = seconds, Nanos = nanos },
            Metadata = ByteString.CopyFrom(Generator.SHA384Hash().Span),
            LedgerId = ByteString.CopyFrom([(byte)Generator.Integer(0, 255)])
        };
        return new Response { TokenGetNftInfo = new TokenGetNftInfoResponse { Nft = nftInfo } };
    }
}
