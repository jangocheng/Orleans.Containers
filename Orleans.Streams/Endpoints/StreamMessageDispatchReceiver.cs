﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Streams.Messages;

namespace Orleans.Streams.Endpoints
{
    public class StreamMessageDispatchReceiver : IStreamMessageVisitor, ITransactionalStreamTearDown
    {
        private readonly Dictionary<Type, List<dynamic>> _callbacks = new Dictionary<Type, List<dynamic>>();
        private readonly IStreamProvider _streamProvider;
        private readonly Func<Task> _tearDownFunc;
        private StreamSubscriptionHandle<IStreamMessage> _streamHandle;
        private bool _tearDownExecuted;

        public StreamMessageDispatchReceiver(IStreamProvider streamProvider, Func<Task> tearDownFunc = null)
        {
            _streamProvider = streamProvider;
            _tearDownFunc = tearDownFunc;
            _tearDownExecuted = false;
        }

        public async Task Visit(IStreamMessage streamMessage)
        {
            List<dynamic> funcList;
            _callbacks.TryGetValue(streamMessage.GetType(), out funcList);

            if (funcList != null)
            {
                foreach (var func in funcList)
                {
                    await func(streamMessage as dynamic);
                }
            }
        }

        public async Task Subscribe(StreamIdentity streamIdentity)
        {
            _tearDownExecuted = false;
            var messageStream = _streamProvider.GetStream<IStreamMessage>(streamIdentity.StreamIdentifier.Item1, streamIdentity.StreamIdentifier.Item2);

            _streamHandle =
                await messageStream.SubscribeAsync((message, token) => Visit(message), async () => await TearDown());
            // TODO evaluate OnNextBatchAsync
        }

        public void Register<T>(Func<T, Task> func)
        {
            var type = typeof (T);
            if (!_callbacks.ContainsKey(type))
            {
                _callbacks.Add(type, new List<dynamic>());
            }

            _callbacks[type].Add(func);
        }

        public async Task TearDown()
        {
            if (!_tearDownExecuted)
            {
                _tearDownExecuted = true;
                if (_streamHandle != null)
                {
                    await _streamHandle.UnsubscribeAsync();
                }

                _streamHandle = null;
                if (_tearDownFunc != null)
                {
                    await _tearDownFunc();
                }
            }
        }

        public Task<bool> IsTearedDown()
        {
            return Task.FromResult(_tearDownExecuted);
        }
    }
}