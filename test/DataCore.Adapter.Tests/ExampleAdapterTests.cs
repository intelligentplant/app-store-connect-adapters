﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    [TestClass]
    public class ExampleAdapterTests : AdapterTests<ExampleAdapter> {

        private static readonly ReadTagValuesQueryDetails TestTag1 = new ReadTagValuesQueryDetails("Test Tag 1");


        protected override ExampleAdapter CreateAdapter() {
            return new ExampleAdapter();
        }


        protected override Task<ReadTagValuesQueryDetails> GetReadTagValuesQueryDetails() {
            return Task.FromResult(TestTag1);
        }


        protected override Task<ReadEventMessagesQueryDetails> GetReadEventMessagesQueryDetails() {
            return Task.FromResult<ReadEventMessagesQueryDetails>(null);
        }


        protected override async Task EmitTestEvent(ExampleAdapter adapter, EventMessageSubscriptionType subscriptionType, string topic) {
            await adapter.WriteTestEventMessage(
                EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(DateTime.UtcNow)
                    .WithCategory(TestContext.FullyQualifiedTestClassName)
                    .WithMessage(TestContext.TestName)
                    .WithPriority(EventPriority.Low)
                    .Build()
            );
        }


        [TestMethod]
        public Task UnsupportedFeatureShouldNotBeFound() {
            return RunAdapterTest((adapter, context) => {
                var feature = adapter.Features.Get<IFakeAdapterFeature>();
                Assert.IsNull(feature);
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task SupportedFeatureShouldBeFound() {
            return RunAdapterTest((adapter, context) => { 
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                Assert.IsNotNull(feature);
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task SnapshotSubscriptionShouldReceiveAdditionalValues() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();


                var subscription = await feature.Subscribe(context, new CreateSnapshotTagValueSubscriptionRequest() { 
                    Tags = new[] { TestTag1.Id }
                }, CancellationToken);

                // Write a couple of values that we should then be able to read out again via 
                // the subscription's channel.
                var now = System.DateTime.UtcNow;
                await adapter.WriteSnapshotValue(
                    TagValueQueryResult.Create(
                        TestTag1.Id,
                        TestTag1.Id,
                        TagValueBuilder
                            .Create()
                            .WithUtcSampleTime(now.AddSeconds(-5))
                            .WithValue(100)
                            .Build()
                    )
                );
                await adapter.WriteSnapshotValue(
                    TagValueQueryResult.Create(
                        TestTag1.Id,
                        TestTag1.Id,
                        TagValueBuilder
                            .Create()
                            .WithUtcSampleTime(now.AddSeconds(-1))
                            .WithValue(99)
                            .Build()
                    )
                );

                // Read initial value.
                using (var ctSource = new CancellationTokenSource(1000)) {
                    var value = await subscription.ReadAsync(ctSource.Token).ConfigureAwait(false);
                    ctSource.Token.ThrowIfCancellationRequested();
                    Assert.IsNotNull(value);
                }

                // Read first value written above.
                using (var ctSource = new CancellationTokenSource(1000)) {
                    var value = await subscription.ReadAsync(ctSource.Token).ConfigureAwait(false);
                    ctSource.Token.ThrowIfCancellationRequested();
                    Assert.AreEqual(now.AddSeconds(-5), value.Value.UtcSampleTime);
                    Assert.AreEqual(100, value.Value.Value.GetValueOrDefault<int>());
                }

                // Read second value written above.
                using (var ctSource = new CancellationTokenSource(1000)) {
                    var value = await subscription.ReadAsync(ctSource.Token).ConfigureAwait(false);
                    ctSource.Token.ThrowIfCancellationRequested();
                    Assert.AreEqual(now.AddSeconds(-1), value.Value.UtcSampleTime);
                    Assert.AreEqual(99, value.Value.Value.GetValueOrDefault<int>());
                }
            });
        }

    }
}
