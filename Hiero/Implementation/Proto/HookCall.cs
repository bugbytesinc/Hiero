using Google.Protobuf;

namespace Proto;

internal static class HookCallExtensions
{
    internal static HookCall ToHookCallProto(this Hiero.HookCall hookCall)
    {
        if (hookCall is null)
        {
            throw new ArgumentNullException(nameof(hookCall), "Hook call is missing. Please check that it is not null.");
        }
        return new HookCall
        {
            HookId = hookCall.HookId,
            EvmHookCall = new Proto.EvmHookCall
            {
                Data = ByteString.CopyFrom(hookCall.Data.Span),
                GasLimit = hookCall.GasLimit
            }
        };
    }
}
