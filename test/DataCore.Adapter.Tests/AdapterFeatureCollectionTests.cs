﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterFeatureCollectionTests {

        [TestMethod]
        public void AdapterFeatureCollectionShouldBePrePopulated() {
            var featureCollection = new AdapterFeaturesCollection(new FeatureProvider());
            Assert.IsNotNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should be defined.");
            Assert.IsNotNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should be defined.");
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldBeEmptyWhenProviderDoesNotImplementFeatures() {
            var featureCollection = new AdapterFeaturesCollection(new object());
            Assert.IsNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should not be defined.");
            Assert.IsNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should not be defined.");
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldBeEmptyWhenProviderIsNull() {
            var featureCollection = new AdapterFeaturesCollection(null);
            Assert.IsNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should not be defined.");
            Assert.IsNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should not be defined.");
        }



        private class FeatureProvider : IReadSnapshotTagValues, IReadEventMessagesForTimeRange {

            ChannelReader<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            ChannelReader<EventMessage> IReadEventMessagesForTimeRange.ReadEventMessages(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }
        }

    }
}