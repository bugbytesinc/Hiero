// SPDX-License-Identifier: Apache-2.0
using Hiero;

namespace Proto;

public sealed partial class KeyList
{
    internal KeyList(Endorsement[] endorsements) : this()
    {
        Keys.AddRange(endorsements.Select(endorsement => new Key(endorsement)));
    }
    internal Endorsement[] ToEndorsements()
    {
        return Keys.Select(key => key.ToEndorsement()).ToArray();
    }
}