// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Hiero;

namespace Proto;

internal static class TokenTransferExtensions
{
    internal static (IReadOnlyList<TokenTransfer>, IReadOnlyList<Hiero.NftTransfer>, Hiero.TreasuryTransfer?) AsTransferLists(this RepeatedField<TokenTransferList> list)
    {
        if (list is { Count: > 0 })
        {
            var tokenList = new List<TokenTransfer>(list.Count);
            var nftList = new List<Hiero.NftTransfer>(list.Count);
            Hiero.TreasuryTransfer? treasuryTransfer = null;
            foreach (var exchanges in list)
            {
                var token = exchanges.Token.AsAddress();
                foreach (var xfer in exchanges.Transfers)
                {
                    tokenList.Add(new TokenTransfer(token, xfer.AccountID.AsAddress(), xfer.Amount));
                }
                foreach (var xfer in exchanges.NftTransfers)
                {
                    if (xfer.SerialNumber == -1)
                    {
                        treasuryTransfer = new Hiero.TreasuryTransfer(token, xfer.SenderAccountID.AsAddress(), xfer.ReceiverAccountID.AsAddress());
                    }
                    else
                    {
                        nftList.Add(new Hiero.NftTransfer(new Hiero.Nft(token, xfer.SerialNumber), xfer.SenderAccountID.AsAddress(), xfer.ReceiverAccountID.AsAddress()));
                    }
                }
            }
            return (tokenList, nftList, treasuryTransfer);
        }
        return ([], [], null);
    }
}