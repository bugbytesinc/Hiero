using Hiero.Mirror;

namespace Hiero.Test.Integration.Fixtures;

public interface IHasTokenBalance
{
    public Task<long?> GetTokenBalanceAsync(EntityId token);
    public Task<TokenHoldingData[]> GetTokenBalancesAsync();
}
