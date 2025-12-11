using Google.Protobuf;
using Grpc.Net.Client;

namespace Hiero.Implementation
{
    internal sealed class ConsensusContextStack : ContextStack<ConsensusContextStack, ConsensusNodeEndpoint>, IConsensusContext
    {
        private ContextValue<ConsensusNodeEndpoint?> _endpoint;
        private ContextValue<EntityId?> _payer;
        private ContextValue<Signatory?> _signatory;
        private ContextValue<long> _feeLimit;
        private ContextValue<TimeSpan> _transactionDuration;
        private ContextValue<int> _retryCount;
        private ContextValue<TimeSpan> _retryDelay;
        private ContextValue<long> _queryTip;
        private ContextValue<int> _signaturePrefixTrimLimit;
        private ContextValue<string?> _memo;
        private ContextValue<bool> _adjustForLocalClockDrift;
        private ContextValue<bool> _throwIfNotSuccess;
        private ContextValue<TransactionId?> _transactionId;
        private ContextValue<Action<IMessage>?> _onSendingRequest;
        private ContextValue<Action<int, IMessage>?> _onResponseReceived;
        public ConsensusNodeEndpoint? Endpoint { get => _endpoint.HasValue ? _endpoint.Value : _parent?.Endpoint; set => _endpoint.Set(value); }
        public EntityId? Payer { get => _payer.HasValue ? _payer.Value : _parent?.Payer; set => _payer.Set(value); }
        public Signatory? Signatory { get => _signatory.HasValue ? _signatory.Value : _parent?.Signatory; set => _signatory.Set(value); }
        public long FeeLimit { get => _feeLimit.HasValue ? _feeLimit.Value : _parent?.FeeLimit ?? default; set => _feeLimit.Set(value); }
        public TimeSpan TransactionDuration { get => _transactionDuration.HasValue ? _transactionDuration.Value : _parent?.TransactionDuration ?? default; set => _transactionDuration.Set(value); }
        public int RetryCount { get => _retryCount.HasValue ? _retryCount.Value : _parent?.RetryCount ?? default; set => _retryCount.Set(value); }
        public TimeSpan RetryDelay { get => _retryDelay.HasValue ? _retryDelay.Value : _parent?.RetryDelay ?? default; set => _retryDelay.Set(value); }
        public long QueryTip { get => _queryTip.HasValue ? _queryTip.Value : _parent?.QueryTip ?? default; set => _queryTip.Set(value); }
        public int SignaturePrefixTrimLimit { get => _signaturePrefixTrimLimit.HasValue ? _signaturePrefixTrimLimit.Value : _parent?.SignaturePrefixTrimLimit ?? default; set => _signaturePrefixTrimLimit.Set(value); }
        public string? Memo { get => _memo.HasValue ? _memo.Value : _parent?.Memo; set => _memo.Set(value); }
        public bool AdjustForLocalClockDrift { get => _adjustForLocalClockDrift.HasValue ? _adjustForLocalClockDrift.Value : _parent?.AdjustForLocalClockDrift ?? default; set => _adjustForLocalClockDrift.Set(value); }
        public bool ThrowIfNotSuccess { get => _throwIfNotSuccess.HasValue ? _throwIfNotSuccess.Value : _parent?.ThrowIfNotSuccess ?? default; set => _throwIfNotSuccess.Set(value); }
        public TransactionId? TransactionId { get => _transactionId.HasValue ? _transactionId.Value : _parent?.TransactionId; set => _transactionId.Set(value); }
        public Action<IMessage>? OnSendingRequest { get => _onSendingRequest.HasValue ? _onSendingRequest.Value : _parent?.OnSendingRequest; set => _onSendingRequest.Set(value); }
        public Action<int, IMessage>? OnResponseReceived { get => _onResponseReceived.HasValue ? _onResponseReceived.Value : _parent?.OnResponseReceived; set => _onResponseReceived.Set(value); }

        private ConsensusContextStack(ConsensusContextStack parent) : base(parent) { }
        public ConsensusContextStack(Func<ConsensusNodeEndpoint, GrpcChannel> channelFactory) : base(channelFactory) { }
        public ConsensusContextStack GetWithAddRef()
        {
            addRef();
            return this;
        }
        public ConsensusContextStack GetConfigured(Action<IConsensusContext> configure)
        {
            var context = new ConsensusContextStack(this);
            configure.Invoke(context);
            return context;
        }
        public override void Reset(string name)
        {
            switch (name)
            {
                case nameof(Endpoint): _endpoint.Reset(); break;
                case nameof(Payer): _payer.Reset(); break;
                case nameof(Signatory): _signatory.Reset(); break;
                case nameof(FeeLimit): _feeLimit.Reset(); break;
                case nameof(RetryCount): _retryCount.Reset(); break;
                case nameof(RetryDelay): _retryDelay.Reset(); break;
                case nameof(QueryTip): _queryTip.Reset(); break;
                case nameof(SignaturePrefixTrimLimit): _signaturePrefixTrimLimit.Reset(); break;
                case nameof(TransactionDuration): _transactionDuration.Reset(); break;
                case nameof(Memo): _memo.Reset(); break;
                case nameof(AdjustForLocalClockDrift): _adjustForLocalClockDrift.Reset(); break;
                case nameof(ThrowIfNotSuccess): _throwIfNotSuccess.Reset(); break;
                case nameof(TransactionId): _transactionId.Reset(); break;
                case nameof(OnSendingRequest): _onSendingRequest.Reset(); break;
                case nameof(OnResponseReceived): _onResponseReceived.Reset(); break;
                default: throw new ArgumentOutOfRangeException(nameof(name), $"'{name}' is not a valid property to reset.");
            }
        }
        /// <summary>
        /// Generates the optional sending request hook method, if configured in the context.
        /// </summary>
        /// <remarks>
        /// Unlike other context methods and properties, ALL the configured handlers in all
        /// of the parent contexts are included in the return value, request handlers can 
        /// be stacked thru contexts.
        /// </remarks>
        /// <param name="context">
        /// Context that may be configured with a sending request callback.
        /// </param>
        /// <returns>
        /// An Action that may or may not delegate to multiple handlers that are called just
        /// before a message is sent to the gossip node grpc endpoint.
        /// </returns>
        internal Action<IMessage> InstantiateOnSendingRequestHandler()
        {
            List<Action<IMessage>>? list = null;
            for (var ctx = this; ctx is not null; ctx = ctx._parent)
            {
                if (ctx._onSendingRequest.HasValue)
                {
                    var handler = ctx._onSendingRequest.Value;
                    if (handler is not null)
                    {
                        (list ??= []).Add(handler);
                    }
                }
            }
            if (list is null || list.Count == 0)
            {
                return NoOpSendingHandler;
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            var handlers = list.ToArray();
            return request =>
            {
                for (var i = 0; i < handlers.Length; i++)
                {
                    handlers[i](request);
                }
            };
        }
        /// <summary>
        /// Generates the optional receiving request hook method, if configured in the context.
        /// </summary>
        /// <remarks>
        /// Unlike other context methods and properties, ALL the configured handlers in all
        /// of the parent contexts are included in the return value, request handlers can 
        /// be stacked thru contexts.
        /// </remarks>
        /// <param name="context">
        /// Context that may be configured with a receiving request callback.
        /// </param>
        /// <returns>
        /// An Action that may or may not delegate to multiple handlers that are called just
        /// after a message is received from the gossip node grpc endpoint.
        /// </returns>
        internal Action<int, IMessage> InstantiateOnResponseReceivedHandler()
        {
            List<Action<int, IMessage>>? list = null;
            for (var ctx = this; ctx is not null; ctx = ctx._parent)
            {
                if (ctx._onResponseReceived.HasValue)
                {
                    var handler = ctx._onResponseReceived.Value;
                    if (handler is not null)
                    {
                        (list ??= []).Add(handler);
                    }
                }
            }
            if (list is null || list.Count == 0)
            {
                return NoOpResponseHandler;
            }
            if (list.Count == 1)
            {
                return list[0];
            }
            var handlers = list.ToArray();
            return (tryNumber, response) =>
            {
                for (var i = 0; i < handlers.Length; i++)
                {
                    handlers[i](tryNumber, response);
                }
            };
        }
        protected override ConsensusNodeEndpoint GetChannelKey()
        {
            return Endpoint ?? throw new InvalidOperationException("The Network Consensus Endpoint has not been configured.");
        }
    }
}
