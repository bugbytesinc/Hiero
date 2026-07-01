// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Google.Protobuf.Collections;
using Hiero;
using System.Runtime.InteropServices;

namespace Com.Hedera.Hapi.Node.Hooks;

internal static class HookStorageEntryExtensions
{
    internal static void AddToProto(this IReadOnlyList<HookStorageEntry> entries, RepeatedField<EvmHookStorageUpdate> updates)
    {
        Dictionary<ReadOnlyMemory<byte>, EvmHookMappingEntries>? mappingGroups = null;
        var count = entries.Count;
        var capacity = updates.Count + count;
        if (updates.Capacity < capacity)
        {
            updates.Capacity = capacity;
        }
        for (var i = 0; i < count; i++)
        {
            var entry = entries[i];
            if (entry.IndexKey is null)
            {
                updates.Add(new EvmHookStorageUpdate
                {
                    StorageSlot = new EvmHookStorageSlot
                    {
                        Key = ByteString.CopyFrom(entry.Key.Span),
                        Value = ByteString.CopyFrom(entry.Value.Span)
                    }
                });
            }
            else
            {
                mappingGroups ??= new Dictionary<ReadOnlyMemory<byte>, EvmHookMappingEntries>(count, ReadOnlyMemoryComparer.Instance);
                ref var group = ref CollectionsMarshal.GetValueRefOrAddDefault(mappingGroups, entry.Key, out var exists);
                if (!exists)
                {
                    group = new EvmHookMappingEntries
                    {
                        MappingSlot = ByteString.CopyFrom(entry.Key.Span)
                    };
                }
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
                group!.Entries.Add(protoEntry);
            }
        }
        if (mappingGroups is not null)
        {
            foreach (var group in mappingGroups.Values)
            {
                updates.Add(new EvmHookStorageUpdate { MappingEntries = group });
            }
        }
    }
    private sealed class ReadOnlyMemoryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        internal static readonly ReadOnlyMemoryComparer Instance = new();

        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            var hash = new HashCode();
            foreach (var b in obj.Span)
            {
                hash.Add(b);
            }
            return hash.ToHashCode();
        }
    }
}
