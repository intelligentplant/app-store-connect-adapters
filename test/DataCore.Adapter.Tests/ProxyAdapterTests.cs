﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class ProxyAdapterTests<TProxy> : AdapterTests<TProxy> where TProxy : class, IAdapterProxy {

        private static bool s_historicalTestEventsInitialized;

        private static DateTime s_historicalTestEventsStartTime;


        protected virtual IEnumerable<string> UnsupportedStandardFeatures => Array.Empty<string>();


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
                var eventMessageManager = ServiceProvider.GetService<InMemoryEventMessageStore>();
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


        protected override async Task EmitTestEvent(TProxy adapter, EventMessageSubscriptionType subscriptionType, string topic) {
            var eventMessageManager = ServiceProvider.GetService<InMemoryEventMessageStore>();
            
            var msg = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(DateTime.UtcNow)
                    .WithCategory(TestContext.FullyQualifiedTestClassName)
                    .WithMessage(TestContext.TestName)
                    .WithPriority(EventPriority.Low)
                    .Build();

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<EventMessage> onPublish = evt => { 
                if (evt.Id.Equals(msg.Id)) {
                    tcs.TrySetResult(true);
                }
            };
            eventMessageManager.Publish += onPublish;

            await eventMessageManager.WriteEventMessages(msg);

            try {
                // Wait for the message to actually be published.
                await tcs.Task;
            }
            finally {
                eventMessageManager.Publish -= onPublish;
            }
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


        [TestMethod]
        public Task ProxyShouldHaveLocalImplementationForAllRemoteStandardFeatures() {
            return RunAdapterTest((proxy, context) => {
                foreach (var featureUriOrName in proxy.RemoteDescriptor.Features) {
                    if (UnsupportedStandardFeatures.Contains(featureUriOrName, StringComparer.OrdinalIgnoreCase)) {
                        continue;
                    }
                    Assert.IsTrue(proxy.HasFeature(featureUriOrName), $"Expected to find local implementation for remote feature: {featureUriOrName}");
                }

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task ProxyShouldHaveLocalImplementationForAllRemoteExtensionFeatures() {
            return RunAdapterTest((proxy, context) => {
                foreach (var featureUriOrName in proxy.RemoteDescriptor.Extensions) {
                    Assert.IsTrue(proxy.HasFeature(featureUriOrName), $"Expected to find local implementation for remote feature: {featureUriOrName}");
                }

                return Task.CompletedTask;
            });
        }

    }
}
