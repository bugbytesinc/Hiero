namespace Hiero.Test.Integration.Fixtures;

public interface IHasCryptoBalance
{
    public Task<ulong> GetCryptoBalanceAsync();
}
