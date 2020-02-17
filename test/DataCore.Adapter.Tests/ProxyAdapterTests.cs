﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class ProxyAdapterTests<TProxy> : AdapterTests<TProxy> where TProxy : class, IAdapterProxy {

        private static bool s_historicalTestEventsInitialized;

        private static DateTime s_historicalTestEventsStartTime;


        protected sealed override TProxy CreateAdapter() {
            return CreateProxy(WebHostStartup.AdapterId);
        }


        protected sealed override ReadTagValuesQueryDetails GetReadTagValuesQueryDetails() {
            var now = DateTime.UtcNow;
            return new ReadTagValuesQueryDetails(WebHostStartup.TestTagId) { 
                HistoryStartTime = now.AddDays(-1),
                HistoryEndTime = now
            };
        }


        protected sealed override ReadEventMessagesQueryDetails GetReadEventMessagesQueryDetails() {
            if (!s_historicalTestEventsInitialized) {
                s_historicalTestEventsInitialized = true;
                var now = DateTime.UtcNow;
                var eventMessageManager = ServiceProvider.GetService<InMemoryEventMessageManager>();
                var messages = Enumerable.Range(-100, 100).Select(x => EventMessageBuilder
                    .Create()
                    .WithUtcEventTime(now.AddMinutes(x))
                    .WithCategory(TestContext.FullyQualifiedTestClassName)
                    .WithMessage($"Test message")
                    .WithPriority(EventPriority.Low)
                    .Build()
                ).ToArray();
                s_historicalTestEventsStartTime = messages.First().UtcEventTime;
                eventMessageManager.WriteEventMessages(messages);
            }

            return new ReadEventMessagesQueryDetails() { 
                HistoryStartTime = s_historicalTestEventsStartTime,
                HistoryEndTime = DateTime.UtcNow
            };
        }


        protected override Task EmitTestEvent(TProxy adapter, EventMessageSubscriptionType subscriptionType) {
            var eventMessageManager = ServiceProvider.GetService<InMemoryEventMessageManager>();
            eventMessageManager.WriteEventMessages(
                EventMessageBuilder
                    .Create()
                    .WithUtcEventTime(DateTime.UtcNow)
                    .WithCategory(TestContext.FullyQualifiedTestClassName)
                    .WithMessage(TestContext.TestName)
                    .WithPriority(EventPriority.Low)
                    .Build()
            );
            return Task.CompletedTask;
        }


        protected abstract TProxy CreateProxy(string remoteAdapterId);



        [TestMethod]
        public Task ProxyShouldRetrieveRemoteAdapterDetails() {
            return RunAdapterTest((proxy, context) => {
                Assert.IsNotNull(proxy.RemoteHostInfo);
                Assert.IsNotNull(proxy.RemoteDescriptor);

                return Task.CompletedTask;
            });
        }

    }
}
