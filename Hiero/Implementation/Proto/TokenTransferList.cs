using Google.Protobuf.Collections;
using Hiero;
using System.Collections.Generic;

namespace Proto;

internal static class TokenTransferExtensions
{
    internal static (IReadOnlyList<TokenTransfer>, IReadOnlyList<Hiero.NftTransfer>) AsTokenAndAssetTransferLists(this RepeatedField<TokenTransferList> list)
    {
        if (list is { Count: > 0 })
        {
            var tokenList = new List<TokenTransfer>(list.Count);
            var assetList = new List<Hiero.NftTransfer>(list.Count);
            foreach (var exchanges in list)
            {
                var token = exchanges.Token.AsAddress();
                foreach (var xfer in exchanges.Transfers)
                {
                    tokenList.Add(new TokenTransfer(token, xfer.AccountID.AsAddress(), xfer.Amount));
                }
                foreach (var xfer in exchanges.NftTransfers)
                {
                    assetList.Add(new Hiero.NftTransfer(new Hiero.Nft(token, xfer.SerialNumber), xfer.SenderAccountID.AsAddress(), xfer.ReceiverAccountID.AsAddress()));
                }
            }
            return (tokenList, assetList);
        }
        return ([], []);
    }
}