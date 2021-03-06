﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Streams.Messages;

namespace Orleans.Streams.Linq.Nodes
{
    internal class StreamProcessorSelectNodeGrain<TIn, TOut> : StreamProcessorNodeGrain<TIn, TOut>, IStreamProcessorSelectNodeGrain<TIn, TOut>
    {
        private Func<TIn, TOut> _function;

        public Task SetFunction(Func<TIn, TOut> function)
        {
            _function = function;
            return TaskDone.Done;
        }

        public override async Task Visit(ItemMessage<TIn> itemMessage)
        {
            var result = itemMessage.Items.Select(_function).ToList();
            await StreamProvider.SendItems(result, false);
        }
    }
}