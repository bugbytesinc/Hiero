using Hiero.Implementation;
using Proto;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Extended Balance information for an Address, including Token Balances.
/// </summary>
public sealed record AccountBalances
{
    /// <summary>
    /// The Hedera address holding the account balance(s).
    /// </summary>
    public EntityId Holder { get; private init; }
    /// <summary>
    /// Address Crypto Balance in Tinybars
    /// </summary>
    public ulong Crypto { get; private init; }
    /// <summary>
    /// [DEPRICATED] Balances of tokens associated with this account.
    /// </summary>
    /// <remarks>
    /// This field is not guaranteed to be populated in the response
    /// with the complete list of holdings of the associated account.
    /// </remarks>
    [Obsolete("This field is deprecated by HIP-367")]
    public ReadOnlyDictionary<EntityId, CryptoBalance> Tokens { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Response
    /// </summary>
    internal AccountBalances(Response response)
    {
        var balances = response.CryptogetAccountBalance;
        Holder = balances.AccountID.AsAddress();
        Crypto = balances.Balance;
        var tokens = new Dictionary<EntityId, CryptoBalance>();
#pragma warning disable CS0612 // Type or member is obsolete
        foreach (var entry in balances.TokenBalances)
        {
            var account = entry.TokenId.AsAddress();
            if (tokens.TryGetValue(account, out CryptoBalance? crypto))
            {
                tokens[account] = crypto with
                {
                    Balance = crypto.Balance + entry.Balance
                };
            }
            else
            {
                tokens[account] = new CryptoBalance
                {
                    Balance = entry.Balance,
                    Decimals = entry.Decimals
                };
            }
        }
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        Tokens = new ReadOnlyDictionary<EntityId, CryptoBalance>(tokens);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractBalancesExtensions
{
    /// <summary>
    /// Retrieves the crypto and token blances from the network for a given contract.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="contract">
    /// The hedera network address of the contract to retrieve the balance of.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// An object containing the crypto balance associated with the
    /// contract in addition to a list of all tokens held by the contract
    /// with their balances.
    /// </returns>
    public static async Task<AccountBalances> GetContractBalancesAsync(this ConsensusClient client, EntityId contract, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new AccountBalances(await Engine.QueryAsync(client, new CryptoGetAccountBalanceQuery { ContractID = new ContractID(contract) }, cancellationToken, configure).ConfigureAwait(false));
    }
    /// <summary>
    /// Retrieves the balance in tinybars from the network for a given contract.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="contract">
    /// The hedera network contract address to retrieve the balance of.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The balance of the associated contract.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<ulong> GetContractBalanceAsync(this ConsensusClient client, EntityId contract, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new AccountBalances(await Engine.QueryAsync(client, new CryptoGetAccountBalanceQuery { ContractID = new ContractID(contract) }, cancellationToken, configure).ConfigureAwait(false)).Crypto;
    }
    /// <summary>
    /// Retrieves the crypto and token blances from the network for a given address.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="address">
    /// The hedera network address to retrieve the balance of.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// An object containing the crypto balance associated with the
    /// account in addition to a list of all tokens held by the account
    /// with their balances.
    /// </returns>
    public static async Task<AccountBalances> GetAccountBalancesAsync(this ConsensusClient client, EntityId address, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new AccountBalances(await Engine.QueryAsync(client, new CryptoGetAccountBalanceQuery { AccountID = new AccountID(address) }, cancellationToken, configure).ConfigureAwait(false));
    }
    /// <summary>
    /// Retrieves the balance in tinybars from the network for a given address.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="address">
    /// The hedera network address to retrieve the balance of.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The balance of the associated address.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<ulong> GetAccountBalanceAsync(this ConsensusClient client, EntityId address, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new AccountBalances(await Engine.QueryAsync(client, new CryptoGetAccountBalanceQuery { AccountID = new AccountID(address) }, cancellationToken, configure).ConfigureAwait(false)).Crypto;
    }
}