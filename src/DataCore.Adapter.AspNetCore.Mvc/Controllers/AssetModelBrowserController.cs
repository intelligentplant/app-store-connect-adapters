﻿using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.AssetModel;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for browsing an adapter's asset model hierarchy.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/asset-model")]
    // Legacy route for compatibility with v1 of the toolkit
    [Route("api/data-core/v1.0/asset-model")] 
    public class AssetModelBrowserController : ControllerBase {

        /// <summary>
        /// The service for accessing the running adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// Maximum number of asset model nodes that will be returned in a single query.
        /// </summary>
        public const int MaxNodesPerQuery = 1000;


        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public AssetModelBrowserController(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Browses the asset model hierarchy. Up to <see cref="MaxNodesPerQuery"/> will be 
        /// returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="start">
        ///   The optional starting node ID. If no value is specified, browsing will start at the 
        ///   top-level nodes in the hierarchy.
        /// </param>
        /// <param name="page">
        ///   The results page to retrieve.
        /// </param>
        /// <param name="pageSize">
        ///   The page size for the query.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/browse")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public Task<IActionResult> BrowseNodes(string adapterId, string? start = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) {
            return BrowseNodesPost(adapterId, new BrowseAssetModelNodesRequest() { 
                ParentId = start,
                PageSize = pageSize,
                Page = page
            }, cancellationToken);
        }


        /// <summary>
        /// Browses the asset model hierarchy. Up to <see cref="MaxNodesPerQuery"/> will be 
        /// returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The browse request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/browse")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> BrowseNodesPost(string adapterId, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelBrowse>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IAssetModelBrowse))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            using (var activity = Telemetry.ActivitySource.StartBrowseAssetModelNodesActivity(resolvedFeature.Adapter.Descriptor.Id, request)) {
                try {
                    var result = new List<AssetModelNode>(MaxNodesPerQuery);

                    var itemsRead = 0;
                    await foreach (var item in feature.BrowseAssetModelNodes(callContext, request, cancellationToken).ConfigureAwait(false)) {
                        if (item != null) {
                            ++itemsRead;
                            result.Add(item);

                            if (itemsRead >= MaxNodesPerQuery) {
                                Util.AddIncompleteResponseHeader(Response, string.Format(callContext.CultureInfo, Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                                break;
                            }
                        }
                    }

                    activity.SetResponseItemCountTag(itemsRead);
                    return Ok(result); // 200
                }
                catch (SecurityException) {
                    return Forbid(); // 403
                }
            }
        }


        /// <summary>
        /// Gets a collection of nodes by ID. Up to <see cref="MaxNodesPerQuery"/> will be returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The request object describing the nodes to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/get-by-id")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> GetNodes(string adapterId, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelBrowse>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IAssetModelBrowse))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            using (var activity = Telemetry.ActivitySource.StartGetAssetModelNodesActivity(resolvedFeature.Adapter.Descriptor.Id, request)) {
                try {
                    var result = new List<AssetModelNode>(MaxNodesPerQuery);

                    var itemsRead = 0;
                    await foreach (var item in feature.GetAssetModelNodes(callContext, request, cancellationToken).ConfigureAwait(false)) {
                        if (item != null) {
                            ++itemsRead;
                            result.Add(item);

                            if (itemsRead >= MaxNodesPerQuery) {
                                Util.AddIncompleteResponseHeader(Response, string.Format(callContext.CultureInfo, Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                                break;
                            }
                        }
                    }

                    activity.SetResponseItemCountTag(itemsRead);
                    return Ok(result); // 200
                }
                catch (SecurityException) {
                    return Forbid(); // 403
                }
            }
        }


        /// <summary>
        /// Finds nodes matching the specified search filter. Up to <see cref="MaxNodesPerQuery"/> 
        /// will be returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The request object describing the nodes to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/find")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> FindNodes(string adapterId, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelSearch>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IAssetModelSearch))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            using (var activity = Telemetry.ActivitySource.StartFindAssetModelNodesActivity(resolvedFeature.Adapter.Descriptor.Id, request)) {
                try {
                    var result = new List<AssetModelNode>(MaxNodesPerQuery);

                    var itemsRead = 0;
                    await foreach (var item in feature.FindAssetModelNodes(callContext, request, cancellationToken).ConfigureAwait(false)) {
                        if (item != null) {
                            ++itemsRead;
                            result.Add(item);

                            if (itemsRead >= MaxNodesPerQuery) {
                                Util.AddIncompleteResponseHeader(Response, string.Format(callContext.CultureInfo, Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                                break;
                            }
                        }
                    }

                    activity.SetResponseItemCountTag(itemsRead);
                    return Ok(result); // 200
                }
                catch (SecurityException) {
                    return Forbid(); // 403
                }
            }
        }

    }
}
