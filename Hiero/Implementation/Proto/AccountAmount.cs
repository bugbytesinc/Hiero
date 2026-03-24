// SPDX-License-Identifier: Apache-2.0
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
    internal AccountAmount(EntityId pseudoAddress, long amount, bool delegated, Hiero.HookCall? allowanceHook) : this()
    {
        AccountID = new AccountID(pseudoAddress);
        Amount = amount;
        IsApproval = delegated;
        if (allowanceHook is not null)
        {
            var protoHook = allowanceHook.ToHookCallProto();
            if (allowanceHook.CallMode == HookCallMode.PreAndPost)
            {
                PrePostTxAllowanceHook = protoHook;
            }
            else
            {
                PreTxAllowanceHook = protoHook;
            }
        }
    }
}
