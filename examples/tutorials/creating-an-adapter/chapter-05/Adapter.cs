﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues {

        private readonly ConcurrentDictionary<string, TagDefinition> _tagsById = new ConcurrentDictionary<string, TagDefinition>();

        private readonly ConcurrentDictionary<string, TagDefinition> _tagsByName = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);


        public Adapter(
            string id,
            string name,
            string description = null,
            IBackgroundTaskService scheduler = null,
            ILogger<Adapter> logger = null
        ) : base(
            id, 
            name, 
            description, 
            scheduler, 
            logger
        ) {
            AddFeature<ISnapshotTagValuePush, PollingSnapshotTagValuePush>(PollingSnapshotTagValuePush.ForAdapter(
                this, 
                TimeSpan.FromSeconds(5)
            ));

            AddFeatures(ReadHistoricalTagValues.ForAdapter(this));
        }


        private Random GetRng(TagDefinition tag, DateTime startAt) {
            return new Random((tag.GetHashCode() + startAt.GetHashCode()).GetHashCode());
        }


        private AdapterProperty CreateMinimumValueProperty(double min) {
            return new AdapterProperty("MinValue", min, "The inclusive minimum value for the tag");
        }


        private AdapterProperty CreateMaximumValueProperty(double max) {
            return new AdapterProperty("MaxValue", max, "The exclusive maximum value for the tag");
        }


        private void CreateTags() {
            for (var i = 0; i < 5; i++) {
                var tagId = (i + 1).ToString();
                var tagName = string.Concat("RandomValue_", tagId);
                // Our tags can have a minimum value of 0 and a maximum value of 1. We'll add 
                // properties to the tag to describe this.
                var tagProperties = new[] { 
                    CreateMinimumValueProperty(0),
                    CreateMaximumValueProperty(1)
                };

                var tag = new TagDefinition(
                    tagId,
                    tagName,
                    "A tag that returns a random value",
                    null,
                    VariantType.Double,
                    null,
                    tagProperties,
                    null
                );

                _tagsById[tag.Id] = tag;
                _tagsByName[tag.Name] = tag;
            }
        }


        private void DeleteTags() {
            _tagsById.Clear();
            _tagsByName.Clear();
        }


        protected override Task StartAsync(CancellationToken cancellationToken) {
            CreateTags();
            AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return Task.CompletedTask;
        }


        protected override Task StopAsync(CancellationToken cancellationToken) {
            DeleteTags();
            return Task.CompletedTask;
        }


        protected override Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(
            IAdapterCallContext context, 
            CancellationToken cancellationToken
        ) {
            return Task.FromResult<IEnumerable<HealthCheckResult>>(new[] {
                HealthCheckResult.Healthy("All systems normal!")
            });
        }


        public ChannelReader<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request, 
            CancellationToken cancellationToken
        ) {
            return new[] {
                CreateMinimumValueProperty(0),
                CreateMaximumValueProperty(1)
            }.OrderBy(x => x.Name).SelectPage(request).PublishToChannel();
        }


        public ChannelReader<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagDefinition>();

            TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }

                    if (_tagsById.TryGetValue(tag, out var t) || _tagsByName.TryGetValue(tag, out t)) {
                        result.Writer.TryWrite(t);
                    }
                }
            }, result.Writer, true, cancellationToken);
            
            return result;
        }


        public ChannelReader<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagDefinition>();

            TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in _tagsById.Values.ApplyFilter(request)) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    ch.TryWrite(tag);
                }
            }, result.Writer, true, cancellationToken);

            return result;
        }


        public ChannelReader<TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context, 
            ReadSnapshotTagValuesRequest request, 
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagValueQueryResult>();

            TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }
                    if (!_tagsById.TryGetValue(tag, out var t) && !_tagsByName.TryGetValue(tag, out t)) {
                        continue;
                    }

                    var now = DateTime.UtcNow;
                    var rnd = GetRng(t, now);

                    ch.TryWrite(new TagValueQueryResult(
                        t.Id,
                        t.Name,
                        TagValueBuilder
                            .Create()
                            .WithUtcSampleTime(now)
                            .WithValue(rnd.NextDouble())
                            .WithStatus(rnd.NextDouble() <= 0.7 ? TagValueStatus.Good : TagValueStatus.Bad)
                            .Build()
                    ));
                }
            }, result.Writer, true, cancellationToken);

            return result;
        }


        public ChannelReader<TagValueQueryResult> ReadRawTagValues(
            IAdapterCallContext context, 
            ReadRawTagValuesRequest request, 
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagValueQueryResult>();

            TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }
                    if (!_tagsById.TryGetValue(tag, out var t) && !_tagsByName.TryGetValue(tag, out t)) {
                        continue;
                    }

                    var intervalRnd = GetRng(t, request.UtcStartTime);
                    var sampleCount = 0;

                    for (var ts = request.UtcStartTime; ts <= request.UtcEndTime && (request.SampleCount < 1 || sampleCount <= request.SampleCount); ts = ts.AddSeconds(intervalRnd.Next(3, 9))) {
                        var rnd = GetRng(t, ts);
                        ch.TryWrite(new TagValueQueryResult(
                            t.Id,
                            t.Name,
                            TagValueBuilder
                                .Create()
                                .WithUtcSampleTime(ts)
                                .WithValue(rnd.NextDouble())
                                .WithStatus(rnd.NextDouble() <= 0.7 ? TagValueStatus.Good : TagValueStatus.Bad)
                                .Build()
                        ));
                    }
                }
                
            }, result.Writer, true, cancellationToken);

            return result;
        }
    }
}
