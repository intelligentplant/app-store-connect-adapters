﻿using System.Linq;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents.
    /// </summary>
    public static class GrpcAssetModelExtensions {

        /// <summary>
        /// Converts a gRPC asset model node to its adapter equivalent.
        /// </summary>
        /// <param name="node">
        ///   The gRPC asset model node.
        /// </param>
        /// <returns>
        ///   The adapter asset model node.
        /// </returns>
        public static AssetModelNode ToAdapterAssetModelNode(this Grpc.AssetModelNode node) {
            if (node == null) {
                return null;
            }

            return AssetModelNode.Create(
                node.Id,
                node.Name,
                node.Description,
                string.IsNullOrWhiteSpace(node.Parent) 
                    ? null 
                    : node.Parent,
                node.Children,
                node.Measurements.Select(x => AssetModelNodeMeasurement.Create(x.Name, x.AdapterId, RealTimeData.TagSummary.Create(x.Tag.Id, x.Tag.Name, x.Tag.Description, x.Tag.Units))).ToArray(),
                node.Properties
            );
        }

    }
}
