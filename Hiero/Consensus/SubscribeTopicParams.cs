﻿using System.Threading.Channels;

namespace Hiero;

public class SubscribeTopicParams
{
    /// <summary>
    /// The Message's Topics.
    /// </summary>
    public EntityId Topic { get; set; } = default!;
    /// <summary>
    /// Optional, filter for messages which reached consensus on or 
    /// after this time. If not set, messages occurring from the
    /// current time forward are returned.
    /// </summary>
    public ConsensusTimeStamp? Starting { get; set; } = null;
    /// <summary>
    /// Optional, filter for messages which reached consensus before 
    /// this time. If not set, it will stream indefinitely.
    /// </summary>
    public ConsensusTimeStamp? Ending { get; set; } = null;
    /// <summary>
    /// Optional, the maximum number of topic messages to return before
    /// completing the call, if set to 0 it will stream messages 
    /// indefinitely until the stream terminates by other means.
    /// </summary>
    public ulong MaxCount { get; set; } = 0;
    /// <summary>
    /// .NET system threadding channel writer receiving messages 
    /// streamed from the server.  Messages can be read from the 
    /// stream by calling code without blocking the incoming 
    /// stream of messages from the mirror node.  Completing 
    /// the stream will close the streaming connection to the 
    /// mirror node and cause this method to return without error.
    /// </summary>
    public ChannelWriter<TopicMessage> MessageWriter { get; set; } = default!;
    /// <summary>
    /// Indicate that the .net channel should be "completed" when
    /// the streaming connection to the mirror node completes, both
    /// for planned and faulted reasons.  Default is true.  Set to false
    /// to re-use the channel or for scenarios where it may be combined
    /// and multiplexed with other channel combinations.
    /// </summary>
    public bool CompleteChannelWhenFinished { get; set; } = true;
    /// <summary>
    /// Optional cancelation token, that when set, closes the mirror node
    /// connection and optionally the .net channel (if configured to do so),
    /// and causes the SubscribeTopic method to return without error.
    /// Default is "none", the method can be completed by signaling the receiving
    /// .net channel as completed.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}