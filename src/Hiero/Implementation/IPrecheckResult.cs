// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero.Implementation;

internal interface IPrecheckResult
{
    public ResponseCodeEnum PrecheckCode { get; }
}
