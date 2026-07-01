// SPDX-License-Identifier: Apache-2.0
using Hiero;

namespace Proto;

public sealed partial class KeyList
{
    internal KeyList(IReadOnlyList<Endorsement> endorsements) : this()
    {
        var count = endorsements.Count;
        if (Keys.Capacity < count)
        {
            Keys.Capacity = count;
        }
        for (var i = 0; i < count; i++)
        {
            Keys.Add(new Key(endorsements[i]));
        }
    }
    internal Endorsement[] ToEndorsements()
    {
        var keys = Keys;
        var count = keys.Count;
        var endorsements = new Endorsement[count];
        for (var i = 0; i < count; i++)
        {
            endorsements[i] = keys[i].ToEndorsement();
        }
        return endorsements;
    }
}
