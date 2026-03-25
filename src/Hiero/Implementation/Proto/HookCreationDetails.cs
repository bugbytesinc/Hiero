// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Proto;

namespace Com.Hedera.Hapi.Node.Hooks;

internal static class HookCreationDetailsExtensions
{
    internal static HookCreationDetails ToProto(this HookMetadata metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata), "Hook metadata is missing. Please check that it is not null.");
        }
        var spec = new EvmHookSpec
        {
            ContractId = new ContractID(metadata.Contract)
        };
        var evmHook = new EvmHook { Spec = spec };
        if (metadata.InitialStorage is not null)
        {
            foreach (var update in metadata.InitialStorage.ToProto())
            {
                evmHook.StorageUpdates.Add(update);
            }
        }
        var details = new HookCreationDetails
        {
            ExtensionPoint = HookExtensionPoint.AccountAllowanceHook,
            HookId = metadata.Id,
            EvmHook = evmHook
        };
        if (metadata.AdminKey is not null)
        {
            details.AdminKey = new Key(metadata.AdminKey);
        }
        return details;
    }
}
