#pragma warning disable CS0612 // Type or member is obsolete
using Hiero.Implementation;

namespace Proto;

public sealed partial class TransactionResponse : IPrecheckResult
{
    ResponseCodeEnum IPrecheckResult.PrecheckCode => nodeTransactionPrecheckCode_;
}