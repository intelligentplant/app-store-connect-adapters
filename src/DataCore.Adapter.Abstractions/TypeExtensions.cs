﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="Type"/> instances.
    /// </summary>
    public static class TypeExtensions {

        /// <summary>
        /// <see cref="IAdapterFeature"/> type.
        /// </summary>
        private static readonly Type s_adapterFeatureType = typeof(IAdapterFeature);

        /// <summary>
        /// <see cref="IAdapterExtensionFeature"/> type.
        /// </summary>
        private static readonly Type s_adapterExtensionFeatureType = typeof(IAdapterExtensionFeature);

        /// <summary>
        /// Array of all standard adapter feature types.
        /// </summary>
        private static readonly Type[] s_standardAdapterFeatureTypes = typeof(IAdapterFeature)
            .Assembly
            .GetTypes()
            .Where(x => x.IsInterface)
            .Where(x => s_adapterFeatureType.IsAssignableFrom(x))
            .Where(x => x != s_adapterFeatureType && x != s_adapterExtensionFeatureType)
            .ToArray();


        /// <summary>
        /// Gets the <see cref="Type"/> objects that correspond to the standard adapter feature 
        /// types.
        /// </summary>
        /// <returns>
        ///   The adapter feature types.
        /// </returns>
        public static Type[] GetStandardAdapterFeatureTypes() {
            return s_standardAdapterFeatureTypes;
        }


        /// <summary>
        /// Tests if the type is an adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is an adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsAdapterFeature(this Type type) {
            if (type == null) {
                return false;
            }

            return type.IsInterface && 
                (s_standardAdapterFeatureTypes.Any(f => f.IsAssignableFrom(type)) || s_adapterExtensionFeatureType.IsAssignableFrom(type)) &&
                type != s_adapterFeatureType && 
                type != s_adapterExtensionFeatureType;
        }


        /// <summary>
        /// Tests if the type is a standard adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is a standard adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsStandardAdapterFeature(this Type type) {
            return type.IsAdapterFeature() && !s_adapterExtensionFeatureType.IsAssignableFrom(type);
        }


        /// <summary>
        /// Tests if the type is an extension adapter feature.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type is an extension adapter feature, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsExtensionAdapterFeature(this Type type) {
            return type.IsAdapterFeature() && s_adapterExtensionFeatureType.IsAssignableFrom(type);
        }

    }
}