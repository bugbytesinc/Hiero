// SPDX-License-Identifier: Apache-2.0
using Com.Hedera.Hapi.Node.Hooks;
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Account creation parameters.
/// </summary>
/// <example>
/// Create a new account funded with 1 HBAR from the client's Payer and
/// authorized by the supplied endorsement:
/// <code source="../../../samples/DocSnippets/CryptoSnippets.cs" region="CreateAccount" language="csharp"/>
/// </example>
public sealed class CreateAccountParams : TransactionParams<CreateAccountReceipt>, INetworkParams<CreateAccountReceipt>
{
    /// <summary>
    /// The public key structure representing the signature or signatures
    /// required to sign on behalf of this new account.  It can represent
    /// a single Ed25519 key, set of n-of-m keys or any other key structure
    /// supported by the network.
    /// </summary>
    public Endorsement? Endorsement { get; set; }
    /// <summary>
    /// The initial balance that will be transferred from the 
    /// <see cref="IConsensusContext.Payer"/> account to the new account 
    /// upon creation.
    /// </summary>
    public ulong InitialBalance { get; set; }
    /// <summary>
    /// When creating a new account: the newly created account must 
    /// sign any transaction transferring crypto into the newly 
    /// created account.
    /// </summary>
    public bool RequireReceiveSignature { get; set; } = false;
    /// <summary>
    /// The maximum number of tokens (fungible or NFT) that this account may
    /// be automatically associated with, which happens when it receives a
    /// token it is not yet associated with.  Use -1 for no limit; 0 means
    /// every token must be associated explicitly.
    /// </summary>
    /// <remarks>
    /// Defaults to zero.
    /// </remarks>
    public int AutoAssociationLimit { get; set; } = 0;
    /// <summary>
    /// The funds of this account will be staked to
    /// the node that this specified account is staked to 
    /// and the specified account will receive the earned reward.
    /// </summary>
    /// <remarks>
    /// This value must be set to <code>null</code> or
    /// <code>None</code> if the <code>StakedNode</code>
    /// property is set.
    /// </remarks>
    public EntityId? ProxyAccount { get; set; } = null;
    /// <summary>
    /// The funds of this account will be staked to
    /// the gossip node with the given ID.
    /// </summary>
    /// <remarks>
    /// Must be <code>null</code> if the <code>ProxyAccount</code>
    /// property is set; any value, including node 0, conflicts with it.
    /// </remarks>
    public long? StakedNode { get; set; } = null;
    /// <summary>
    /// Indicate to the network that this account
    /// does not wish to receive any earned 
    /// staking rewards.
    /// </summary>
    public bool DeclineStakeReward { get; set; } = false;
    /// <summary>
    /// The auto-renew period for the newly created account, it will continue 
    /// to be renewed at the given interval for as long as the account contains 
    /// hbars sufficient to cover the renewal charge.
    /// </summary>
    public TimeSpan AutoRenewPeriod { get; set; } = TimeSpan.FromSeconds(7890000);
    /// <summary>
    /// Optional Alias value to use to identify this newly created account.
    /// </summary>
    public Endorsement? KeyAlias { get; set; } = null;
    /// <summary>
    /// Short description of the account.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// Optional list of hooks to create on the account immediately
    /// after it is created.
    /// </summary>
    public IReadOnlyList<HookMetadata>? Hooks { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to create this account.  Typically matches the
    /// Endorsement assigned to this new account.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<CreateAccountReceipt>.CreateNetworkTransaction()
    {
        if (Endorsement.IsNullOrNone())
        {
            throw new ArgumentOutOfRangeException(nameof(Endorsement), "The Endorsement for the account is missing, it is required.");
        }
        if (AutoAssociationLimit < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(AutoAssociationLimit), "The maximum number of auto-associations must equal to or greater than -1.");
        }
        var result = new CryptoCreateTransactionBody();
        if (ProxyAccount.IsNullOrNone())
        {
            if (StakedNode.HasValue)
            {
                result.StakedNodeId = StakedNode.Value;
            }
        }
        else if (StakedNode.HasValue)
        {
            throw new ArgumentNullException(nameof(ProxyAccount), "Both the ProxyAccount and StakedNode properties are specified, only one can be set.");
        }
        else
        {
            result.StakedAccountId = new AccountID(ProxyAccount);
        }
        result.Key = new Key(Endorsement);
        result.InitialBalance = InitialBalance;
        result.ReceiverSigRequired = RequireReceiveSignature;
        result.AutoRenewPeriod = new Duration(AutoRenewPeriod);
        if (!KeyAlias.IsNullOrNone())
        {
            result.Alias = new Key(KeyAlias).ToByteString();
        }
        result.DeclineReward = DeclineStakeReward;
        result.Memo = Memo ?? string.Empty;
        result.MaxAutomaticTokenAssociations = AutoAssociationLimit;
        if (Hooks is not null)
        {
            var count = Hooks.Count;
            var hookCreationDetails = result.HookCreationDetails;
            if (hookCreationDetails.Capacity < count)
            {
                hookCreationDetails.Capacity = count;
            }
            for (var i = 0; i < count; i++)
            {
                hookCreationDetails.Add(Hooks[i].ToProto());
            }
        }
        return result;
    }
    CreateAccountReceipt INetworkParams<CreateAccountReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new CreateAccountReceipt(transactionId, receipt);
    }
    string INetworkParams<CreateAccountReceipt>.OperationDescription => "Create Account";
}
/// <summary>
/// Extension methods for creating cryptocurrency accounts.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateAccountExtensions
{
    /// <summary>
    /// Creates a new network account with a given initial balance
    /// and other values as indicated in the create parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the create.
    /// </param>
    /// <param name="createParameters">
    /// The account creation parameters, includes the initial balance,
    /// public key and values associated with the new account.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt with a description of the newly created account.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/CryptoSnippets.cs" region="CreateAccount" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<CreateAccountReceipt> CreateAccountAsync(this ConsensusClient client, CreateAccountParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(createParameters, configure);
    }
}
