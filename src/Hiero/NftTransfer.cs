// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Represents a NFT transfer.
/// </summary>
public sealed record NftTransfer
{
    /// <summary>
    /// The Token ID and Serial Number of the nft to transfer.
    /// </summary>
    public Nft Nft { get; private init; }
    /// <summary>
    /// The account sending the NFT.
    /// </summary>
    public EntityId Sender { get; private init; }
    /// <summary>
    /// The account receiving the NFT.
    /// </summary>
    public EntityId Receiver { get; private init; }
    /// <summary>
    /// Indicates the transfer was authorized through the
    /// allowance mechanism and the sending account
    /// may not have signed this transaction.
    /// </summary>
    public bool Delegated { get; private init; }
    /// <summary>
    /// Optional allowance hook call for the sender of this
    /// NFT transfer. The hook's <see cref="HookCall.CallMode"/>
    /// determines whether it is invoked before the transfer
    /// only, or both before and after.
    /// </summary>
    public HookCall? SenderAllowanceHook { get; private init; }
    /// <summary>
    /// Optional allowance hook call for the receiver of this
    /// NFT transfer. The hook's <see cref="HookCall.CallMode"/>
    /// determines whether it is invoked before the transfer
    /// only, or both before and after.
    /// </summary>
    public HookCall? ReceiverAllowanceHook { get; private init; }
    /// <summary>
    /// Public Constructor, an <code>NftTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="nft">
    /// The address and serial number of the nft to transfer.
    /// </param>
    /// <param name="sender">
    /// The address of the crypto account having the NFT.
    /// </param>
    /// <param name="receiver">
    /// The address of the crypto account receiving the NFT.
    /// </param>
    /// <param name="delegated">
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </param>
    /// <param name="senderAllowanceHook">
    /// Optional allowance hook call for the sender.
    /// </param>
    /// <param name="receiverAllowanceHook">
    /// Optional allowance hook call for the receiver.
    /// </param>
    public NftTransfer(Nft nft, EntityId sender, EntityId receiver, bool delegated = false, HookCall? senderAllowanceHook = null, HookCall? receiverAllowanceHook = null)
    {
        Nft = nft;
        Sender = sender;
        Receiver = receiver;
        Delegated = delegated;
        SenderAllowanceHook = senderAllowanceHook;
        ReceiverAllowanceHook = receiverAllowanceHook;
    }
}
