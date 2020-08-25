﻿using System.Collections.Generic;
using System.Threading.Tasks;

using DataCore.Adapter.Http.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class HttpProxyTests : ProxyAdapterTests<HttpAdapterProxy> {

        protected override IEnumerable<string> UnsupportedStandardFeatures {
            get {
                // HTTP proxy does not support any push-based features.
                yield return WellKnownFeatures.Events.EventMessagePush;
                yield return WellKnownFeatures.Events.EventMessagePushWithTopics;
                yield return WellKnownFeatures.RealTimeData.SnapshotTagValuePush;
            }
        }

        protected override HttpAdapterProxy CreateProxy(string remoteAdapterId) {
            return ActivatorUtilities.CreateInstance<HttpAdapterProxy>(ServiceProvider, nameof(HttpProxyTests), new HttpAdapterProxyOptions() {
                RemoteId = remoteAdapterId
            });
        }

    }

}
