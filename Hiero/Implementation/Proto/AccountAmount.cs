using Hiero;

namespace Proto;

public sealed partial class AccountAmount
{
    internal AccountAmount(EntityId psudoAddress, long amount, bool delegated) : this()
    {
        AccountID = new AccountID(psudoAddress);
        Amount = amount;
        IsApproval = delegated;
    }
}