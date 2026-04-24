// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>transactiontype</c> query parameter
/// of the <c>/api/v1/transactions</c> list endpoint — narrows the
/// listing to transactions of a particular HAPI type.
/// </summary>
/// <remarks>
/// Exposed as a closed set of static properties (one per HAPI
/// transaction-type wire value) rather than a factory taking an
/// enum. The static-property form is AOT-safe — nothing reflects
/// on an enum member name at runtime — and yields the same
/// IntelliSense experience at the call site. The wire string is
/// pinned per property so evolving the set is a single
/// additive change in this file.
/// </remarks>
public sealed class TransactionTypeFilter : IMirrorFilter
{
    /// <summary>Filter for <c>ATOMICBATCH</c> transactions.</summary>
    public static readonly TransactionTypeFilter AtomicBatch = new("ATOMICBATCH");
    /// <summary>Filter for <c>CONSENSUSCREATETOPIC</c> transactions.</summary>
    public static readonly TransactionTypeFilter ConsensusCreateTopic = new("CONSENSUSCREATETOPIC");
    /// <summary>Filter for <c>CONSENSUSDELETETOPIC</c> transactions.</summary>
    public static readonly TransactionTypeFilter ConsensusDeleteTopic = new("CONSENSUSDELETETOPIC");
    /// <summary>Filter for <c>CONSENSUSSUBMITMESSAGE</c> transactions.</summary>
    public static readonly TransactionTypeFilter ConsensusSubmitMessage = new("CONSENSUSSUBMITMESSAGE");
    /// <summary>Filter for <c>CONSENSUSUPDATETOPIC</c> transactions.</summary>
    public static readonly TransactionTypeFilter ConsensusUpdateTopic = new("CONSENSUSUPDATETOPIC");
    /// <summary>Filter for <c>CONTRACTCALL</c> transactions.</summary>
    public static readonly TransactionTypeFilter ContractCall = new("CONTRACTCALL");
    /// <summary>Filter for <c>CONTRACTCREATEINSTANCE</c> transactions.</summary>
    public static readonly TransactionTypeFilter ContractCreateInstance = new("CONTRACTCREATEINSTANCE");
    /// <summary>Filter for <c>CONTRACTDELETEINSTANCE</c> transactions.</summary>
    public static readonly TransactionTypeFilter ContractDeleteInstance = new("CONTRACTDELETEINSTANCE");
    /// <summary>Filter for <c>CONTRACTUPDATEINSTANCE</c> transactions.</summary>
    public static readonly TransactionTypeFilter ContractUpdateInstance = new("CONTRACTUPDATEINSTANCE");
    /// <summary>Filter for <c>CRYPTOADDLIVEHASH</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoAddLiveHash = new("CRYPTOADDLIVEHASH");
    /// <summary>Filter for <c>CRYPTOAPPROVEALLOWANCE</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoApproveAllowance = new("CRYPTOAPPROVEALLOWANCE");
    /// <summary>Filter for <c>CRYPTOCREATEACCOUNT</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoCreateAccount = new("CRYPTOCREATEACCOUNT");
    /// <summary>Filter for <c>CRYPTODELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoDelete = new("CRYPTODELETE");
    /// <summary>Filter for <c>CRYPTODELETEALLOWANCE</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoDeleteAllowance = new("CRYPTODELETEALLOWANCE");
    /// <summary>Filter for <c>CRYPTODELETELIVEHASH</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoDeleteLiveHash = new("CRYPTODELETELIVEHASH");
    /// <summary>Filter for <c>CRYPTOTRANSFER</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoTransfer = new("CRYPTOTRANSFER");
    /// <summary>Filter for <c>CRYPTOUPDATEACCOUNT</c> transactions.</summary>
    public static readonly TransactionTypeFilter CryptoUpdateAccount = new("CRYPTOUPDATEACCOUNT");
    /// <summary>Filter for <c>ETHEREUMTRANSACTION</c> transactions.</summary>
    public static readonly TransactionTypeFilter EthereumTransaction = new("ETHEREUMTRANSACTION");
    /// <summary>Filter for <c>FILEAPPEND</c> transactions.</summary>
    public static readonly TransactionTypeFilter FileAppend = new("FILEAPPEND");
    /// <summary>Filter for <c>FILECREATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter FileCreate = new("FILECREATE");
    /// <summary>Filter for <c>FILEDELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter FileDelete = new("FILEDELETE");
    /// <summary>Filter for <c>FILEUPDATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter FileUpdate = new("FILEUPDATE");
    /// <summary>Filter for <c>FREEZE</c> transactions.</summary>
    public static readonly TransactionTypeFilter Freeze = new("FREEZE");
    /// <summary>Filter for <c>HOOKSTORE</c> transactions.</summary>
    public static readonly TransactionTypeFilter HookStore = new("HOOKSTORE");
    /// <summary>Filter for <c>LEDGERIDPUBLICATION</c> transactions.</summary>
    public static readonly TransactionTypeFilter LedgerIdPublication = new("LEDGERIDPUBLICATION");
    /// <summary>Filter for <c>NODECREATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter NodeCreate = new("NODECREATE");
    /// <summary>Filter for <c>NODEDELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter NodeDelete = new("NODEDELETE");
    /// <summary>Filter for <c>NODESTAKEUPDATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter NodeStakeUpdate = new("NODESTAKEUPDATE");
    /// <summary>Filter for <c>NODEUPDATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter NodeUpdate = new("NODEUPDATE");
    /// <summary>Filter for <c>REGISTEREDNODECREATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter RegisteredNodeCreate = new("REGISTEREDNODECREATE");
    /// <summary>Filter for <c>REGISTEREDNODEDELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter RegisteredNodeDelete = new("REGISTEREDNODEDELETE");
    /// <summary>Filter for <c>REGISTEREDNODEUPDATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter RegisteredNodeUpdate = new("REGISTEREDNODEUPDATE");
    /// <summary>Filter for <c>SCHEDULECREATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter ScheduleCreate = new("SCHEDULECREATE");
    /// <summary>Filter for <c>SCHEDULEDELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter ScheduleDelete = new("SCHEDULEDELETE");
    /// <summary>Filter for <c>SCHEDULESIGN</c> transactions.</summary>
    public static readonly TransactionTypeFilter ScheduleSign = new("SCHEDULESIGN");
    /// <summary>Filter for <c>SYSTEMDELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter SystemDelete = new("SYSTEMDELETE");
    /// <summary>Filter for <c>SYSTEMUNDELETE</c> transactions.</summary>
    public static readonly TransactionTypeFilter SystemUndelete = new("SYSTEMUNDELETE");
    /// <summary>Filter for <c>TOKENAIRDROP</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenAirdrop = new("TOKENAIRDROP");
    /// <summary>Filter for <c>TOKENASSOCIATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenAssociate = new("TOKENASSOCIATE");
    /// <summary>Filter for <c>TOKENBURN</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenBurn = new("TOKENBURN");
    /// <summary>Filter for <c>TOKENCANCELAIRDROP</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenCancelAirdrop = new("TOKENCANCELAIRDROP");
    /// <summary>Filter for <c>TOKENCLAIMAIRDROP</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenClaimAirdrop = new("TOKENCLAIMAIRDROP");
    /// <summary>Filter for <c>TOKENCREATION</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenCreation = new("TOKENCREATION");
    /// <summary>Filter for <c>TOKENDELETION</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenDeletion = new("TOKENDELETION");
    /// <summary>Filter for <c>TOKENDISSOCIATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenDissociate = new("TOKENDISSOCIATE");
    /// <summary>Filter for <c>TOKENFEESCHEDULEUPDATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenFeeScheduleUpdate = new("TOKENFEESCHEDULEUPDATE");
    /// <summary>Filter for <c>TOKENFREEZE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenFreeze = new("TOKENFREEZE");
    /// <summary>Filter for <c>TOKENGRANTKYC</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenGrantKyc = new("TOKENGRANTKYC");
    /// <summary>Filter for <c>TOKENMINT</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenMint = new("TOKENMINT");
    /// <summary>Filter for <c>TOKENPAUSE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenPause = new("TOKENPAUSE");
    /// <summary>Filter for <c>TOKENREJECT</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenReject = new("TOKENREJECT");
    /// <summary>Filter for <c>TOKENREVOKEKYC</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenRevokeKyc = new("TOKENREVOKEKYC");
    /// <summary>Filter for <c>TOKENUNFREEZE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenUnfreeze = new("TOKENUNFREEZE");
    /// <summary>Filter for <c>TOKENUNPAUSE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenUnpause = new("TOKENUNPAUSE");
    /// <summary>Filter for <c>TOKENUPDATE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenUpdate = new("TOKENUPDATE");
    /// <summary>Filter for <c>TOKENUPDATENFTS</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenUpdateNfts = new("TOKENUPDATENFTS");
    /// <summary>Filter for <c>TOKENWIPE</c> transactions.</summary>
    public static readonly TransactionTypeFilter TokenWipe = new("TOKENWIPE");
    /// <summary>Filter for <c>UNCHECKEDSUBMIT</c> transactions.</summary>
    public static readonly TransactionTypeFilter UncheckedSubmit = new("UNCHECKEDSUBMIT");
    /// <summary>Filter for <c>UTILPRNG</c> transactions.</summary>
    public static readonly TransactionTypeFilter UtilPrng = new("UTILPRNG");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "transactiontype";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private TransactionTypeFilter(string value) => Value = value;
}
