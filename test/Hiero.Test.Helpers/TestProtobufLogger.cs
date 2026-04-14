// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using TUnit.Core;

namespace Hiero.Test.Helpers;

/// <summary>
/// Bridges the SDK's OnSendingRequest/OnResponseReceived hooks
/// to TUnit's per-test output via TestContext.Current.
/// </summary>
public static class TestProtobufLogger
{
    private static readonly JsonFormatter Formatter = new(JsonFormatter.Settings.Default);

    /// <summary>
    /// Creates a callback for OnSendingRequest that writes the
    /// protobuf message as JSON to the current test's output.
    /// </summary>
    public static Action<IMessage> CreateSendingLogger(string? prefix = null)
    {
        return message =>
        {
            var context = TestContext.Current;
            if (context is null) return;

            var label = prefix is null ? "[REQUEST]" : $"[{prefix} REQUEST]";
            context.OutputWriter.WriteLine($"{label} {message.Descriptor.Name}");
            context.OutputWriter.WriteLine(Formatter.Format(message));
            context.OutputWriter.WriteLine("---");
        };
    }

    /// <summary>
    /// Creates a callback for OnResponseReceived that writes the
    /// protobuf message as JSON to the current test's output.
    /// </summary>
    public static Action<int, IMessage> CreateResponseLogger(string? prefix = null)
    {
        return (retry, message) =>
        {
            var context = TestContext.Current;
            if (context is null) return;

            var label = prefix is null ? $"[RESPONSE #{retry}]" : $"[{prefix} RESPONSE #{retry}]";
            context.OutputWriter.WriteLine($"{label} {message.Descriptor.Name}");
            context.OutputWriter.WriteLine(Formatter.Format(message));
            context.OutputWriter.WriteLine("---");
        };
    }

    /// <summary>
    /// Attaches both sending and response loggers to a consensus context.
    /// </summary>
    public static void AttachProtobufLogging(IConsensusContext context, string? prefix = null)
    {
        context.OnSendingRequest = CreateSendingLogger(prefix);
        context.OnResponseReceived = CreateResponseLogger(prefix);
    }
}
