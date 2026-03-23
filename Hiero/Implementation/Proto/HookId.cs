using Hiero;

namespace Proto;

public sealed partial class HookId
{
    internal HookId(Hook hook) : this()
    {
        if (hook is null)
        {
            throw new ArgumentNullException(nameof(hook), "Hook is missing. Please check that it is not null.");
        }
        EntityId = new HookEntityId(hook.Owner);
        HookId_ = hook.Id;
    }
}

public sealed partial class HookEntityId
{
    internal HookEntityId(EntityId entity) : this()
    {
        if (entity is null)
        {
            throw new ArgumentNullException(nameof(entity), "Hook entity is missing. Please check that it is not null.");
        }
        AccountId = new AccountID(entity);
    }
}

internal static class HookIdExtensions
{
    internal static Hook AsHook(this HookId? hookId)
    {
        if (hookId?.EntityId is not null)
        {
            var entity = hookId.EntityId.EntityIdCase switch
            {
                HookEntityId.EntityIdOneofCase.AccountId => hookId.EntityId.AccountId.AsAddress(),
                HookEntityId.EntityIdOneofCase.ContractId => hookId.EntityId.ContractId.AsAddress(),
                _ => EntityId.None
            };
            return new Hook(entity, hookId.HookId_);
        }
        return Hook.None;
    }
}
