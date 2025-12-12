using Hiero;

namespace Proto;

public sealed partial class AccountAmount
{
    internal AccountAmount(EntityId pseudoAddress, long amount, bool delegated) : this()
    {
        AccountID = new AccountID(pseudoAddress);
        Amount = amount;
        IsApproval = delegated;
    }
}