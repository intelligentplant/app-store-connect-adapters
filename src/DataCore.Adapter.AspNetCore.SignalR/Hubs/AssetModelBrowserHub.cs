﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.AssetModel;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for asset model browsing.

    public partial class AdapterHub {

        /// <summary>
        /// Browses nodes in an adapter's asset model hierarchy.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async IAsyncEnumerable<AssetModelNode> BrowseAssetModelNodes(string adapterId, BrowseAssetModelNodesRequest request, [EnumeratorCancellation] CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartBrowseAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.BrowseAssetModelNodes(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++outputItems;
                        yield return item;
                    }
                }
                finally {
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }
        }


        /// <summary>
        /// Gets nodes in an adapter's asset model hierarchy by ID.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async IAsyncEnumerable<AssetModelNode> GetAssetModelNodes(string adapterId, GetAssetModelNodesRequest request, [EnumeratorCancellation] CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);


            using (Telemetry.ActivitySource.StartGetAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.GetAssetModelNodes(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++outputItems;
                        yield return item;
                    }
                }
                finally {
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }
        }


        /// <summary>
        /// Finds nodes in an adapter's asset model hierarchy that match the specified search filters.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async IAsyncEnumerable<AssetModelNode> FindAssetModelNodes(string adapterId, FindAssetModelNodesRequest request, [EnumeratorCancellation] CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelSearch>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartFindAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.FindAssetModelNodes(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++outputItems;
                        yield return item;
                    }
                }
                finally {
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }
        }

    }
}
