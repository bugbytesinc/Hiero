// SPDX-License-Identifier: Apache-2.0
using Hiero;

namespace Proto;

public sealed partial class TimestampSeconds
{
    internal ConsensusTimeStamp ToConsensusTimeStamp()
    {
        return new ConsensusTimeStamp(Seconds);
    }
}