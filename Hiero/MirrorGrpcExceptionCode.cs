using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Status codes identifying the type of error encountered during mirror gRPC streaming.
/// </summary>
public enum MirrorGrpcExceptionCode
{
    /// <summary>
    /// An entity exists with the specified address
    /// but it is not a topic.
    /// </summary>
    [Description("Not a Topic")] InvalidTopicAddress = 1,
    /// <summary>
    /// No entity was found to exist at the
    /// specified address.
    /// </summary>
    [Description("Not Found")] TopicNotFound = 2,
    /// <summary>
    /// The Mirror Node is presently not available
    /// to service requests.
    /// </summary>
    [Description("Unavailable")] Unavailable = 3,
    /// <summary>
    /// The Mirror Node returned an unknown error
    /// code or suffered a gRPC communication error.
    /// </summary>
    [Description("Other Communication Error")] CommunicationError = 4,
}