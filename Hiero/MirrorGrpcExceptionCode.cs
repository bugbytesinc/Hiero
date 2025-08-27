﻿using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Pre-Check and Receipt Response Codes - 1to1 mapping with protobuf ResponseCodeEnum
/// </summary>
public enum MirrorGrpcExceptionCode
{
    /// <summary>
    /// An entity exists with the specified Payer
    /// but it is not a topic.
    /// </summary>
    [Description("Not a Topic")] InvalidTopicAddress = 1,
    /// <summary>
    /// No entity was found to exist at the 
    /// specified Payer.
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