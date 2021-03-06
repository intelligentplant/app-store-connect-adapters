﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.AssetModel;

using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="AssetModelBrowserService.AssetModelBrowserServiceBase"/>
    /// </summary>
    public class AssetModelBrowserServiceImpl : AssetModelBrowserService.AssetModelBrowserServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public AssetModelBrowserServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task BrowseAssetModelNodes(BrowseAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.BrowseAssetModelNodesRequest() {
                ParentId = string.IsNullOrWhiteSpace(request.ParentId) 
                    ? null 
                    : request.ParentId,
                PageSize = request.PageSize,
                Page = request.Page,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartBrowseAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = adapter.Feature.BrowseAssetModelNodes(adapterCallContext, adapterRequest, cancellationToken);

                long outputItems = 0;
                await foreach (var node in reader.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                    if (node == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(node.ToGrpcAssetModelNode()).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task GetAssetModelNodes(GetAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.GetAssetModelNodesRequest() {
                Nodes = request.Nodes.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartGetAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = adapter.Feature.GetAssetModelNodes(adapterCallContext, adapterRequest, cancellationToken);

                long outputItems = 0;
                await foreach (var node in reader.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                    if (node == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(node.ToGrpcAssetModelNode()).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task FindAssetModelNodes(FindAssetModelNodesRequest request, IServerStreamWriter<AssetModelNode> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IAssetModelSearch>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.AssetModel.FindAssetModelNodesRequest() {
                Name = request.Name,
                Description = request.Description,
                PageSize = request.PageSize,
                Page = request.Page,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartFindAssetModelNodesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = adapter.Feature.FindAssetModelNodes(adapterCallContext, adapterRequest, cancellationToken);

                long outputItems = 0;
                await foreach (var node in reader.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                    if (node == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(node.ToGrpcAssetModelNode()).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
            }
        }

    }
}
