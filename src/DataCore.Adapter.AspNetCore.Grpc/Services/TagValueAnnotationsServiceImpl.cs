﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.RealTimeData;
using DataCore.Adapter.RealTimeData;

using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="TagValueAnnotationsService.TagValueAnnotationsServiceBase"/>.
    /// </summary>
    public class TagValueAnnotationsServiceImpl : TagValueAnnotationsService.TagValueAnnotationsServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationsServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public TagValueAnnotationsServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task ReadAnnotations(ReadAnnotationsRequest request, IServerStreamWriter<TagValueAnnotationQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadAnnotationsRequest() {
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                Tags = request.Tags?.ToArray() ?? Array.Empty<string>(),
                AnnotationCount = request.MaxAnnotationCount,
                Properties = request.Properties
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartReadAnnotationsActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.ReadAnnotations(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcTagValueAnnotationQueryResult()).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task<TagValueAnnotation> ReadAnnotation(ReadAnnotationRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadAnnotationRequest() {
                Tag = request.Tag,
                AnnotationId = request.AnnotationId,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartReadAnnotationActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var result = await adapter.Feature.ReadAnnotation(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                activity.SetResponseItemCountTag(result == null ? 0 : 1);
                return result!.ToGrpcTagValueAnnotation();
            }
        }


        /// <inheritdoc/>
        public override async Task<WriteTagValueAnnotationResult> CreateAnnotation(CreateAnnotationRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.CreateAnnotationRequest() {
                Tag = request.Tag,
                Annotation = request.Annotation.ToAdapterTagValueAnnotation(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartCreateAnnotationActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var result = await adapter.Feature.CreateAnnotation(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
            }
        }


        /// <inheritdoc/>
        public override async Task<WriteTagValueAnnotationResult> UpdateAnnotation(UpdateAnnotationRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.UpdateAnnotationRequest() {
                Tag = request.Tag,
                AnnotationId = request.AnnotationId,
                Annotation = request.Annotation.ToAdapterTagValueAnnotation(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartUpdateAnnotationActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var result = await adapter.Feature.UpdateAnnotation(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
            }
        }


        /// <inheritdoc/>
        public override async Task<WriteTagValueAnnotationResult> DeleteAnnotation(DeleteAnnotationRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.DeleteAnnotationRequest() {
                Tag = request.Tag,
                AnnotationId = request.AnnotationId,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartDeleteAnnotationActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var result = await adapter.Feature.DeleteAnnotation(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
            }
        }

    }
}
