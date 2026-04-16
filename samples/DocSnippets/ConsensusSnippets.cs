// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the Consensus (HCS) domain. See
// CryptoSnippets.cs for the authoring convention. The basic create-topic,
// submit-message, and subscribe flows live in samples/ConsensusService and
// samples/TopicSubscription — this file covers the params-object overloads
// and the segmented-message workflow.

using System.Text;
using System.Threading.Channels;
using Hiero;

namespace DocSnippets;

public static class ConsensusSnippets
{
    public static async Task CreateTopicWithAdmin(
        ConsensusClient client,
        Endorsement adminEndorsement,
        Endorsement submitterEndorsement)
    {
        #region CreateTopicWithKeys
        // Create a topic with an admin key (required to update or delete it)
        // and a separate submit key (required on every SubmitMessage call).
        // Both key-holders must sign CreateTopic.
        var receipt = await client.CreateTopicAsync(new CreateTopicParams
        {
            Memo = "Gated topic",
            Administrator = adminEndorsement,
            Submitter = submitterEndorsement,
            RenewPeriod = TimeSpan.FromDays(90)
        });
        Console.WriteLine($"Topic: {receipt.Topic}");
        #endregion
    }

    public static async Task SubmitMessageWithParams(
        ConsensusClient client,
        EntityId topic,
        Signatory submitKey)
    {
        #region SubmitMessageWithSubmitKey
        // Submit a message to a submit-key-protected topic. The submit key
        // goes on the params' Signatory — the simple overload has no place
        // to attach it, so the params overload is required here.
        var payload = Encoding.UTF8.GetBytes("Authorized message");
        var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = topic,
            Message = payload,
            Signatory = submitKey
        });
        Console.WriteLine($"Sequence: {receipt.SequenceNumber}");
        #endregion
    }

    public static async Task SubmitSegmentedMessage(
        ConsensusClient client,
        EntityId topic,
        ReadOnlyMemory<byte> largePayload)
    {
        #region SubmitMessageSegmented
        // Messages larger than ~4KB must be split into segments. On segment 1,
        // leave ParentTransactionId null — the SDK computes it for you and
        // returns it via the receipt. Pass that TransactionId as the
        // ParentTransactionId on every subsequent segment so the mirror node
        // can reassemble them into a single logical message.
        const int chunkSize = 4000;
        var totalSegments = (int)Math.Ceiling((double)largePayload.Length / chunkSize);

        var first = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = topic,
            Message = largePayload.Slice(0, Math.Min(chunkSize, largePayload.Length)),
            SegmentIndex = 1,
            TotalSegmentCount = totalSegments
        });

        for (int i = 1; i < totalSegments; i++)
        {
            var offset = i * chunkSize;
            var length = Math.Min(chunkSize, largePayload.Length - offset);
            await client.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = topic,
                Message = largePayload.Slice(offset, length),
                ParentTransactionId = first.TransactionId,
                SegmentIndex = i + 1,
                TotalSegmentCount = totalSegments
            });
        }
        #endregion
    }

    public static async Task UpdateTopic(
        ConsensusClient client, EntityId topic, string newMemo)
    {
        #region UpdateTopic
        // Update mutable topic properties. Null fields are left unchanged.
        // Requires the topic's Administrator key to sign; topics created
        // without one are immutable.
        var receipt = await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = topic,
            Memo = newMemo
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task DeleteTopic(ConsensusClient client, EntityId topic)
    {
        #region DeleteTopic
        // Permanently delete a topic. Requires the Administrator key.
        // After deletion, historical messages remain on mirror nodes but
        // no new messages can be submitted.
        var receipt = await client.DeleteTopicAsync(topic);
        Console.WriteLine($"Delete status: {receipt.Status}");
        #endregion
    }

    public static async Task SubscribeBoundedRange(
        MirrorGrpcClient mirror,
        EntityId topic,
        ConsensusTimeStamp startInclusive,
        ConsensusTimeStamp endExclusive)
    {
        #region SubscribeBoundedRange
        // Subscribe to a bounded time range of messages — the subscription
        // call returns as soon as `Ending` is reached. Useful for backfilling
        // historical messages without streaming indefinitely.
        var channel = Channel.CreateUnbounded<TopicMessage>();
        var subscribe = mirror.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = topic,
            Starting = startInclusive,
            Ending = endExclusive,
            MessageWriter = channel.Writer
        });

        await foreach (var msg in channel.Reader.ReadAllAsync())
        {
            Console.WriteLine(
                $"[{msg.SequenceNumber}] {msg.Consensus}: " +
                Encoding.UTF8.GetString(msg.Message.Span));
        }
        await subscribe;
        #endregion
    }
}
