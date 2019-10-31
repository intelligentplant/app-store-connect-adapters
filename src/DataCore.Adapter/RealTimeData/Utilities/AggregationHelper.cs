﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData.Utilities {
    /// <summary>
    /// Utility class for performing data aggregation (e.g. if a data source does not natively 
    /// support aggregation).
    /// </summary>
    public static class AggregationHelper {

        #region [ Interpolate ]

        /// <summary>
        /// Calculates the interpolated value at the start time of the provided bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="currentBucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of the values used in the calculation.
        /// </remarks>
        private static IEnumerable<TagValueExtended> CalculateInterpolated(TagDefinition tag, TagValueBucket currentBucket) {
            TagValueExtended sample0 = null;
            TagValueExtended sample1 = null;

            if (currentBucket.RawSamples.Count == 0) {
                // No samples in the current bucket. We can still extrapolate a value if we have 
                // at least two samples in the PreBucketSamples collection.

                if (currentBucket.PreBucketSamples.Count == 2) {
                    sample0 = currentBucket.PreBucketSamples[0];
                    sample1 = currentBucket.PreBucketSamples[1];
                }
                else if (currentBucket.PreBucketSamples.Count > 2) {
                    var preBucketSamples = currentBucket.PreBucketSamples.Reverse().Take(2).ToArray();
                    // Samples were reversed; more-recent sample will be at index 0.
                    sample0 = preBucketSamples[1];
                    sample1 = preBucketSamples[0];
                }
            }
            else {
                // We have samples in the current bucket. First, check if the first sample in the 
                // bucket exactly matches the bucket start time. If so, we can return this value 
                // directly without having to compute anything.

                sample1 = currentBucket.RawSamples[0];
                if (sample1.UtcSampleTime == currentBucket.UtcStart) {
                    return new[] { sample1 };
                }

                if (currentBucket.PreBucketSamples.Count > 0) {
                    // We have at least one usable sample from the pre-bucket samples collection 
                    // that we can use as the earlier sample in our interpolation.

                    sample0 = currentBucket.PreBucketSamples.Last();
                }
                else if (currentBucket.RawSamples.Count > 1) {
                    // If we have more than one sample in the current bucket, we will extrapolate 
                    // backwards from the first two samples to the bucket start time.

                    sample0 = sample1;
                    sample1 = currentBucket.RawSamples[1];
                }
            }

            var val = InterpolationHelper.GetValueAtTime(
                tag, 
                currentBucket.UtcStart, 
                sample0, 
                sample1, 
                InterpolationCalculationType.Interpolate
            );

            return val == null
                ? Array.Empty<TagValueExtended>()
                : new[] { val };
        }

        #endregion

        #region [ Average ]

        /// <summary>
        /// Calculates the average value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="currentBucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="currentBucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValueExtended> CalculateAverage(TagDefinition tag, TagValueBucket currentBucket) {
            if (currentBucket.RawSamples.Count == 0) {
                return Array.Empty<TagValueExtended>();
            }

            var tagInfoSample = currentBucket.RawSamples.First();
            var numericValue = currentBucket.RawSamples.Min(x => x.Value.GetValueOrDefault(double.NaN));
            var status = currentBucket.RawSamples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(currentBucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
                    .Build()
            };
        }

        #endregion

        #region [ Min ]

        /// <summary>
        /// Calculates the minimum value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="currentBucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="currentBucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValueExtended> CalculateMinimum(TagDefinition tag, TagValueBucket currentBucket) {
            if (currentBucket.RawSamples.Count == 0) {
                return Array.Empty<TagValueExtended>();
            }

            var tagInfoSample = currentBucket.RawSamples.First();
            var numericValue = currentBucket.RawSamples.Min(x => x.Value.GetValueOrDefault(double.NaN));
            var status = currentBucket.RawSamples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(currentBucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
                    .Build()
            };
        }

        #endregion

        #region [ Max ]

        /// <summary>
        /// Calculates the maximum value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="currentBucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="currentBucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValueExtended> CalculateMaximum(TagDefinition tag, TagValueBucket currentBucket) {
            if (currentBucket.RawSamples.Count == 0) {
                return Array.Empty<TagValueExtended>();
            }

            var tagInfoSample = currentBucket.RawSamples.First();
            var numericValue = currentBucket.RawSamples.Max(x => x.Value.GetValueOrDefault(double.NaN));
            var status = currentBucket.RawSamples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(currentBucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
                    .Build()
            };
        }

        #endregion

        #region [ Count ]

        /// <summary>
        /// Returns a value describing the number of raw samples in the provided bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="currentBucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateCount(TagDefinition tag, TagValueBucket currentBucket) {
            var numericValue = currentBucket.RawSamples.Count();

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(currentBucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(TagValueStatus.Good)
                    .Build()
            };
        }

        #endregion

        #region [ Range ]

        /// <summary>
        /// Calculates the absolute difference between the minimum and maximum values in the 
        /// specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="currentBucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="currentBucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValueExtended> CalculateRange(TagDefinition tag, TagValueBucket currentBucket) {
            if (currentBucket.RawSamples.Count == 0) {
                return null;
            }

            var tagInfoSample = currentBucket.RawSamples.First();
            var minValue = currentBucket.RawSamples.Min(x => x.Value.GetValueOrDefault(double.NaN));
            var maxValue = currentBucket.RawSamples.Max(x => x.Value.GetValueOrDefault(double.NaN));
            var numericValue = Math.Abs(maxValue = minValue);

            var status = currentBucket.RawSamples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(currentBucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
                    .Build()
            };
        }

        #endregion

        #region [ Aggregation using Data Function Names ]

        /// <summary>
        /// Performs aggregation on raw tag values.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions to apply to the raw data.
        /// </param>
        /// <param name="utcStartTime">
        ///   The start time for the data query.
        /// </param>
        /// <param name="utcEndTime">
        ///   The end time for the data query.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval for the data query.
        /// </param>
        /// <param name="rawData">
        ///   The channel that will provide the raw data for the aggregation calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated values.
        /// </returns>
        public static ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(TagDefinition tag, IEnumerable<string> dataFunctions, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler, CancellationToken cancellationToken = default) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }

            var result = Channel.CreateBounded<ProcessedTagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            var funcs = new Dictionary<string, AggregateCalculator>();

            if (dataFunctions != null) {
                foreach (var item in dataFunctions) {
                    if (string.IsNullOrWhiteSpace(item)) {
                        continue;
                    }

                    if (string.Equals(item, DefaultDataFunctions.Interpolate.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Interpolate.Id] = CalculateInterpolated;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Average.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Average.Id] = CalculateAverage;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Maximum.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Maximum.Id] = CalculateMaximum;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Minimum.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Minimum.Id] = CalculateMinimum;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Count.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Count.Id] = CalculateCount;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Range.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Range.Id] = CalculateCount;
                    }
                }
            }

            if (funcs.Count == 0) {
                result.Writer.TryComplete();
                return result;
            }
            
            result.Writer.RunBackgroundOperation((ch, ct) => GetAggregatedValues(tag, utcStartTime, utcEndTime, sampleInterval, rawData, ch, funcs, ct), true, scheduler, cancellationToken);
            return result;
        }


        /// <summary>
        /// Performs aggregation on raw tag values.
        /// </summary>
        /// <param name="tags">
        ///   The tags in the query.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions to apply to the raw data.
        /// </param>
        /// <param name="utcStartTime">
        ///   The start time for the data query.
        /// </param>
        /// <param name="utcEndTime">
        ///   The end time for the data query.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval for the data query.
        /// </param>
        /// <param name="rawData">
        ///   The channel that will provide the raw data for the aggregation calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated values.
        /// </returns>
        public static ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(IEnumerable<TagDefinition> tags, IEnumerable<string> dataFunctions, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler, CancellationToken cancellationToken = default) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }

            Channel<ProcessedTagValueQueryResult> result;

            if (!tags.Any()) {
                // No tags; create the channel and return.
                result = Channel.CreateUnbounded<ProcessedTagValueQueryResult>();
                result.Writer.TryComplete();
                return result;
            }

            if (tags.Count() == 1) {
                // Single tag; use the optimised single-tag overload.
                return GetAggregatedValues(
                    tags.First(),
                    dataFunctions,
                    utcStartTime,
                    utcEndTime,
                    sampleInterval,
                    rawData,
                    scheduler,
                    cancellationToken
                );
            }

            var funcs = new Dictionary<string, AggregateCalculator>();

            if (dataFunctions != null) {
                foreach (var item in dataFunctions) {
                    if (string.IsNullOrWhiteSpace(item)) {
                        continue;
                    }

                    if (string.Equals(item, DefaultDataFunctions.Interpolate.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Interpolate.Id] = CalculateInterpolated;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Average.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Average.Id] = CalculateAverage;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Maximum.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Maximum.Id] = CalculateMaximum;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Minimum.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Minimum.Id] = CalculateMinimum;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Count.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Count.Id] = CalculateCount;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Range.Id, StringComparison.Ordinal)) {
                        funcs[DefaultDataFunctions.Range.Id] = CalculateCount;
                    }
                }
            }

            if (funcs.Count == 0) {
                // No aggregate functions specified; complete the channel and return.
                result = Channel.CreateUnbounded<ProcessedTagValueQueryResult>();
                result.Writer.TryComplete();
                return result;
            }

            // Multiple tags; create a single result channel, and create individual input channels 
            // for each tag in the request and redirect each value emitted from the raw data channel 
            // into the appropriate per-tag input channel.

            result = Channel.CreateBounded<ProcessedTagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false
            });

            var tagLookupById = tags.ToDictionary(x => x.Id);

            var tagRawDataChannels = tags.ToDictionary(x => x.Id, x => Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            }));

            // Redirect values from input channel to per-tag channel.
            rawData.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (!tagRawDataChannels.TryGetValue(val.TagId, out var perTagChannel)) {
                            continue;
                        }

                        if (await perTagChannel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            perTagChannel.Writer.TryWrite(val);
                        }
                    }
                }
                catch (Exception e) {
                    foreach (var item in tagRawDataChannels.Values) {
                        item.Writer.TryComplete(e);
                    }
                    throw;
                }
                finally {
                    foreach (var item in tagRawDataChannels.Values) {
                        item.Writer.TryComplete();
                    }
                }
            }, scheduler, cancellationToken);

            // Execute stream for each tag in the query and write all values into the shared 
            // result channel.
            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await Task.WhenAll(
                    tagRawDataChannels.Select(x => GetAggregatedValues(
                        tagLookupById[x.Key],
                        utcStartTime,
                        utcEndTime,
                        sampleInterval,
                        x.Value,
                        ch,
                        funcs,
                        ct
                    ))
                ).WithCancellation(ct).ConfigureAwait(false);
            }, true, scheduler, cancellationToken);

            return result;
        }


        private static async Task GetAggregatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<ProcessedTagValueQueryResult> resultChannel, IDictionary<string, AggregateCalculator> funcs, CancellationToken cancellationToken) {
            var requiresPreBucketSampleTransfer = funcs.ContainsKey(DefaultDataFunctions.Interpolate.Id);
            
            var bucket = new TagValueBucket() {
                UtcStart = utcStartTime.Subtract(sampleInterval),
                UtcEnd = utcStartTime
            };

            var iterations = 0;

            while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!rawData.TryRead(out var val)) {
                    break;
                }

                ++iterations;

                if (val == null) {
                    continue;
                }

                if (val.Value.UtcSampleTime < bucket.UtcStart) {
                    continue;
                }

                if (val.Value.UtcSampleTime > bucket.UtcEnd) {
                    await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, cancellationToken).ConfigureAwait(false);
                    
                    // Calculate the start/end time for the new bucket that the sample we received 
                    // should go into.

                    var ticks = sampleInterval.Ticks * (val.Value.UtcSampleTime.Ticks / sampleInterval.Ticks);
                    var nextBucketStartTime = new DateTime(ticks, DateTimeKind.Utc);

                    var newBucket = new TagValueBucket() {
                        UtcStart = nextBucketStartTime,
                        UtcEnd = nextBucketStartTime.Add(sampleInterval)
                    };

                    // Now, copy over the two latest samples out of the RawSamples and PreBucketSamples 
                    // for the old bucket into the PreBucketSamples for the new bucket. This is to 
                    // help with the calculation of interpolated data if required.

                    if (requiresPreBucketSampleTransfer) {
                        if (bucket.RawSamples.Count == 2) {
                            foreach (var sample in bucket.RawSamples) {
                                newBucket.PreBucketSamples.Add(sample);
                            }
                        }
                        else if (bucket.RawSamples.Count > 2) {
                            foreach (var sample in bucket.RawSamples.Reverse().Take(2).Reverse()) {
                                newBucket.PreBucketSamples.Add(sample);
                            }
                        }
                        else {
                            foreach (var sample in bucket.PreBucketSamples.Concat(bucket.RawSamples).Reverse().Take(2).Reverse()) {
                                newBucket.PreBucketSamples.Add(sample);
                            }
                        }
                    }

                    bucket = newBucket;
                }

                if (val.Value.UtcSampleTime < utcEndTime) {
                    bucket.RawSamples.Add(val.Value);
                }
            }

            await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, cancellationToken).ConfigureAwait(false);
        }


        private static async Task CalculateAndEmitBucketSamples(TagDefinition tag, TagValueBucket bucket, ChannelWriter<ProcessedTagValueQueryResult> resultChannel, IDictionary<string, AggregateCalculator> funcs, CancellationToken cancellationToken) {
            foreach (var agg in funcs) {
                var vals = agg.Value.Invoke(tag, bucket);
                if (vals == null || !vals.Any()) {
                    continue;
                }

                foreach (var val in vals) {
                    if (val != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(ProcessedTagValueQueryResult.Create(tag.Id, tag.Name, val, agg.Key));
                    }
                }
            }
        }


        /// <summary>
        /// Gets the descriptors for the data functions supported by the <see cref="AggregationHelper"/>.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions() {
            return new[] {
                DefaultDataFunctions.Interpolate,
                DefaultDataFunctions.Average,
                DefaultDataFunctions.Maximum,
                DefaultDataFunctions.Minimum,
                DefaultDataFunctions.Count,
                DefaultDataFunctions.Range
            };
        }

        #endregion

        #region [ Inner Types ]

        /// <summary>
        /// Calculates the aggregated values for the specified bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag that the calculation is being performed for.
        /// </param>
        /// <param name="bucket">
        ///   The bucket to calculate values for.
        /// </param>
        /// <returns>
        ///   The calculated values for the bucket.
        /// </returns>
        private delegate IEnumerable<TagValueExtended> AggregateCalculator(TagDefinition tag, TagValueBucket bucket);

        #endregion

    }
}
