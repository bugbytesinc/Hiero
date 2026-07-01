// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Hiero;

namespace Proto;

internal static class TokenTransferExtensions
{
    internal static (IReadOnlyList<TokenTransfer>, IReadOnlyList<Hiero.NftTransfer>, Hiero.TreasuryTransfer?) AsTransferLists(this RepeatedField<TokenTransferList> list)
    {
        var count = list?.Count ?? 0;
        if (count > 0)
        {
            var tokenCount = 0;
            var nftCount = 0;
            for (var i = 0; i < count; i++)
            {
                var exchanges = list![i];
                tokenCount += exchanges.Transfers.Count;
                var nftTransfers = exchanges.NftTransfers;
                var nftTransferCount = nftTransfers.Count;
                for (var j = 0; j < nftTransferCount; j++)
                {
                    var xfer = nftTransfers[j];
                    if (xfer.SerialNumber != -1)
                    {
                        nftCount++;
                    }
                }
            }
            var tokenList = tokenCount == 0 ? [] : new TokenTransfer[tokenCount];
            var nftList = nftCount == 0 ? [] : new Hiero.NftTransfer[nftCount];
            var tokenIndex = 0;
            var nftIndex = 0;
            Hiero.TreasuryTransfer? treasuryTransfer = null;
            for (var i = 0; i < count; i++)
            {
                var exchanges = list![i];
                var token = exchanges.Token.AsAddress();
                var transfers = exchanges.Transfers;
                var transferCount = transfers.Count;
                for (var j = 0; j < transferCount; j++)
                {
                    var xfer = transfers[j];
                    tokenList[tokenIndex++] = new TokenTransfer(token, xfer.AccountID.AsAddress(), xfer.Amount);
                }
                var nftTransfers = exchanges.NftTransfers;
                var nftTransferCount = nftTransfers.Count;
                for (var j = 0; j < nftTransferCount; j++)
                {
                    var xfer = nftTransfers[j];
                    if (xfer.SerialNumber == -1)
                    {
                        treasuryTransfer = new Hiero.TreasuryTransfer(token, xfer.SenderAccountID.AsAddress(), xfer.ReceiverAccountID.AsAddress());
                    }
                    else
                    {
                        nftList[nftIndex++] = new Hiero.NftTransfer(new Hiero.Nft(token, xfer.SerialNumber), xfer.SenderAccountID.AsAddress(), xfer.ReceiverAccountID.AsAddress());
                    }
                }
            }
            return (tokenList, nftList, treasuryTransfer);
        }
        return ([], [], null);
    }
}
