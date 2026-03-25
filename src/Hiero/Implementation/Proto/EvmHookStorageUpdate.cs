// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero;

namespace Com.Hedera.Hapi.Node.Hooks;

internal static class HookStorageEntryExtensions
{
    internal static IEnumerable<EvmHookStorageUpdate> ToProto(this IEnumerable<HookStorageEntry> entries)
    {
        var mappingGroups = new Dictionary<string, (ReadOnlyMemory<byte> Slot, List<HookStorageEntry> Entries)>();
        foreach (var entry in entries)
        {
            if (entry.IndexKey is null)
            {
                yield return new EvmHookStorageUpdate
                {
                    StorageSlot = new EvmHookStorageSlot
                    {
                        Key = ByteString.CopyFrom(entry.Key.Span),
                        Value = ByteString.CopyFrom(entry.Value.Span)
                    }
                };
            }
            else
            {
                var groupKey = Convert.ToHexString(entry.Key.Span);
                if (!mappingGroups.TryGetValue(groupKey, out var group))
                {
                    group = (entry.Key, new List<HookStorageEntry>());
                    mappingGroups[groupKey] = group;
                }
                group.Entries.Add(entry);
            }
        }
        foreach (var group in mappingGroups.Values)
        {
            var mappingEntries = new EvmHookMappingEntries
            {
                MappingSlot = ByteString.CopyFrom(group.Slot.Span)
            };
            foreach (var entry in group.Entries)
            {
                var protoEntry = new EvmHookMappingEntry
                {
                    Value = ByteString.CopyFrom(entry.Value.Span)
                };
                if (entry.IsPreimage)
                {
                    protoEntry.Preimage = ByteString.CopyFrom(entry.IndexKey!.Value.Span);
                }
                else
                {
                    protoEntry.Key = ByteString.CopyFrom(entry.IndexKey!.Value.Span);
                }
                mappingEntries.Entries.Add(protoEntry);
            }
            yield return new EvmHookStorageUpdate { MappingEntries = mappingEntries };
        }
    }
}
