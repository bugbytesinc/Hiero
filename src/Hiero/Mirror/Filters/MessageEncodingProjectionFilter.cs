// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>encoding</c> query parameter —
/// controls how the mirror node encodes each HCS topic message's
/// payload in the returned JSON. Implements
/// <see cref="IMirrorProjection"/>: it does not narrow which
/// records are returned, only reshapes each response payload.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>encoding=base64</c>; selecting
/// <see cref="Utf8"/> asks the server to decode the message bytes
/// as UTF-8 text before returning them. The latter is valid only
/// when the payload actually is UTF-8 — for binary payloads the
/// server may return null or reject the request.
/// </para>
/// <para>
/// Accepted by <c>/api/v1/topics/{topicId}/messages</c>.
/// </para>
/// </remarks>
public sealed class MessageEncodingProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Return each message payload as a base64-encoded string (the
    /// server's default behavior — explicit here for call-site clarity).
    /// </summary>
    public static readonly MessageEncodingProjectionFilter Base64 = new("base64");
    /// <summary>
    /// Return each message payload as plain UTF-8 text. Valid only
    /// when the payload bytes actually are UTF-8; non-text payloads
    /// may yield a null field or a server error.
    /// </summary>
    public static readonly MessageEncodingProjectionFilter Utf8 = new("utf-8");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "encoding";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private MessageEncodingProjectionFilter(string value) => Value = value;
}
