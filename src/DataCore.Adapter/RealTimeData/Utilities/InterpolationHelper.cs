﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Utility class for creating a data set containing interpolated data from a set of raw tag values.
    /// </summary>
    public static class InterpolationHelper {

        /// <summary>
        /// Interpolates a value between two numeric samples.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The time stamp for the interpolated sample.
        /// </param>
        /// <param name="valueBefore">
        ///   The closest raw sample before <paramref name="utcSampleTime"/>.
        /// </param>
        /// <param name="valueAfter">
        ///   The closest raw sample after <paramref name="utcSampleTime"/>.
        /// </param>
        /// <returns>
        ///   The interpolated sample.
        /// </returns>
        private static TagValue InterpolateSample(DateTime utcSampleTime, TagValue valueBefore, TagValue valueAfter) {
            // If either value is not numeric, we'll just return the earlier value with the requested 
            // sample time. This is to allow "interpolation" of state-based values.
            if (double.IsNaN(valueBefore.NumericValue) || double.IsNaN(valueAfter.NumericValue) || double.IsInfinity(valueBefore.NumericValue) || double.IsInfinity(valueAfter.NumericValue)) {
                return TagValueBuilder.CreateFromExisting(valueBefore)
                    .WithUtcSampleTime(utcSampleTime)
                    .Build();
            }

            var x0 = valueBefore.UtcSampleTime.Ticks;
            var x1 = valueAfter.UtcSampleTime.Ticks;

            var y0 = valueBefore.NumericValue;
            var y1 = valueAfter.NumericValue;

            var nextNumericValue = y0 + (utcSampleTime.Ticks - x0) * ((y1 - y0) / (x1 - x0));
            var nextTextValue = TagValueBuilder.GetTextValue(nextNumericValue);
            var nextStatusValue = new[] { valueBefore, valueAfter }.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return TagValueBuilder.Create()
                .WithUtcSampleTime(utcSampleTime)
                .WithNumericValue(nextNumericValue)
                .WithTextValue(nextTextValue)
                .WithStatus(nextStatusValue)
                .WithUnits(valueBefore.Units)
                .WithNotes($"Interpolated using samples @ {valueBefore.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z and {valueAfter.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z.")
                .Build();
        }


        /// <summary>
        /// Calculates a tag value at the specified time stamp.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcSampleTime">
        ///   The UTC sample time for the calculated sample.
        /// </param>
        /// <param name="valueBefore">
        ///   The raw sample immediately before <paramref name="utcSampleTime"/>.
        /// </param>
        /// <param name="valueAfter">
        ///   The raw sample immediately after <paramref name="utcSampleTime"/>.
        /// </param>
        /// <param name="interpolationType">
        ///   The type of calculation type to perform when calculating the value. Specify 
        ///   <see cref="InterpolationCalculationType.UsePreviousValue"/> for non-numeric or state-based 
        ///   tags and <see cref="InterpolationCalculationType.Interpolate"/> for numeric tags.
        /// </param>
        /// <returns>
        ///   The calculated <see cref="TagValue"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="valueBefore"/> and <paramref name="valueAfter"/> are both 
        ///   <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcSampleTime"/> is earlier than both <paramref name="valueBefore"/> and 
        ///   <paramref name="valueAfter"/>.
        /// </exception>
        public static TagValue GetValueAtTime(TagDefinition tag, DateTime utcSampleTime, TagValue valueBefore, TagValue valueAfter, InterpolationCalculationType interpolationType) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (valueBefore == null && valueAfter == null) {
                throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSample);
            }

            if (valueBefore == null) {
                return utcSampleTime < valueAfter.UtcSampleTime
                    ? throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime, nameof(valueAfter))
                    : TagValueBuilder.CreateFromExisting(valueAfter).WithUtcSampleTime(utcSampleTime).Build();
            }

            if (valueAfter == null) {
                return utcSampleTime < valueBefore.UtcSampleTime
                    ? throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime, nameof(valueBefore))
                    : TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
            }

            if (utcSampleTime < valueBefore.UtcSampleTime && utcSampleTime < valueAfter.UtcSampleTime) {
                throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime);
            }

            if (valueBefore.UtcSampleTime > valueAfter.UtcSampleTime) {
                var tmp = valueBefore;
                valueAfter = valueBefore;
                valueBefore = tmp;
            }

            return interpolationType == InterpolationCalculationType.Interpolate
                ? tag.DataType == TagDataType.Numeric
                    ? InterpolateSample(utcSampleTime, valueBefore, valueAfter)
                    : TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build()
                : TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
        }


        public static ChannelReader<TagValueQueryResult> GetInterpolatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, CancellationToken cancellationToken = default) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }

            var result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            result.Writer.RunBackgroundOperation((ch, ct) => GetInterpolatedValues(tag, GetSampleTimes(utcStartTime, utcEndTime, sampleInterval), InterpolationCalculationType.Interpolate, rawData, ch, ct), true, cancellationToken);

            return result;
        }


        public static ChannelReader<TagValueQueryResult> GetValuesAtSampleTimes(TagDefinition tag, IEnumerable<DateTime> utcSampleTimes, ChannelReader<TagValueQueryResult> rawData, CancellationToken cancellationToken = default) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcSampleTimes == null) {
                throw new ArgumentNullException(nameof(utcSampleTimes));
            }

            var result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            result.Writer.RunBackgroundOperation((ch, ct) => GetInterpolatedValues(tag, utcSampleTimes.OrderBy(x => x), InterpolationCalculationType.UsePreviousValue, rawData, ch, ct), true, cancellationToken);

            return result;
        }


        internal static IEnumerable<DateTime> GetSampleTimes(DateTime utcStart, DateTime utcEnd, TimeSpan sampleInterval) {
            var currentSampleTime = utcStart;

            while (currentSampleTime < utcEnd) {
                yield return currentSampleTime;
                currentSampleTime = currentSampleTime.Add(sampleInterval);
            }

            yield return utcEnd;
        }


        private static async Task GetInterpolatedValues(TagDefinition tag, IEnumerable<DateTime> utcSampleTimes, InterpolationCalculationType interpolationCalculationType, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<TagValueQueryResult> resultChannel, CancellationToken cancellationToken) {
            var sampleTimesEnumerator = utcSampleTimes.GetEnumerator();
            try {
                if (!sampleTimesEnumerator.MoveNext()) {
                    return;
                }

                var nextSampleTime = sampleTimesEnumerator.Current;
                TagValue previousValue = null;
                TagValue previousPreviousValue = null;
                var sampleTimesRemaining = true;

                while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!rawData.TryRead(out var val)) {
                        break;
                    }
                    if (val == null || !sampleTimesRemaining) {
                        continue;
                    }

                    previousPreviousValue = previousValue;
                    previousValue = val.Value;

                    if (previousValue.UtcSampleTime > nextSampleTime && previousPreviousValue != null) {
                        var interpolatedValue = GetValueAtTime(tag, nextSampleTime, previousPreviousValue, previousValue, interpolationCalculationType);
                        if (sampleTimesEnumerator.MoveNext()) {
                            nextSampleTime = sampleTimesEnumerator.Current;
                        }
                        else {
                            sampleTimesRemaining = false;
                        }
                        
                        if (await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                            resultChannel.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, interpolatedValue));
                        }
                    }
                }

                // If the last interpolated point we calculated has a time stamp earlier than the requested 
                // end time (e.g. if the end time was later than the last raw sample), we'll calculate an 
                // additional point for the utcEndTime, based on the two most-recent raw values we processed.  
                if (!cancellationToken.IsCancellationRequested &&
                    sampleTimesRemaining &&
                    previousPreviousValue != null &&
                    previousValue != null) {

                    var interpolatedValue = GetValueAtTime(tag, nextSampleTime, previousPreviousValue, previousValue, interpolationCalculationType);
                    if (await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, interpolatedValue));
                    }
                }
            }
            finally {
                sampleTimesEnumerator.Dispose();
            }
        }

    }


    /// <summary>
    /// Defines the type of calculation to perform when interpolating tag values.
    /// </summary>
    public enum InterpolationCalculationType {

        /// <summary>
        /// The new value should be interpolated using the samples immediately before and immediately 
        /// after the timestamp for the new value. Recommended for numeric tag values.
        /// </summary>
        Interpolate,

        /// <summary>
        /// The new value should repeat the value from the raw sample immediately before the timestamp 
        /// for the new value. Recommended for non-numeric and state-based tag values.
        /// </summary>
        UsePreviousValue

    }
}
