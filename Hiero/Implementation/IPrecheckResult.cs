using Proto;

namespace Hiero.Implementation;

internal interface IPrecheckResult
{
    public ResponseCodeEnum PrecheckCode { get; }
}
