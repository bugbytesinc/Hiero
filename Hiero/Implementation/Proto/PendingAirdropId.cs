using Hiero;

namespace Proto;

public sealed partial class PendingAirdropId
{
    internal PendingAirdropId(Airdrop airdrop) : this()
    {
        if (airdrop.Sender.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(airdrop.Sender), "Airdrop sender is missing. Please check that it is not null or empty.");
        }
        if (airdrop.Receiver.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(airdrop.Receiver), "Airdrop receiver is missing. Please check that it is not null or empty.");
        }
        SenderId = new AccountID(airdrop.Sender);
        ReceiverId = new AccountID(airdrop.Receiver);
        if (airdrop.Nft is not null)
        {
            NonFungibleToken = new NftID(airdrop.Nft);
        }
        else if (!airdrop.Token.IsNullOrNone())
        {
            FungibleTokenType = new TokenID(airdrop.Token!);
        }
        else
        {
            throw new ArgumentException("An airdrop must identify either a fungible token or an NFT.", nameof(airdrop));
        }
    }
}
