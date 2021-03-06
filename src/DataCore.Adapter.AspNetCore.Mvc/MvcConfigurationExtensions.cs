﻿#if NETSTANDARD2_0
using DataCore.Adapter.NewtonsoftJson;
#else
using DataCore.Adapter.Json;
#endif

using System;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class MvcConfigurationExtensions {

        /// <summary>
        /// Adds the adapter API controllers to the MVC registration.
        /// </summary>
        /// <param name="builder">
        ///   The MVC builder.
        /// </param>
        /// <returns>
        ///   The MVC builder.
        /// </returns>
        public static IMvcBuilder AddDataCoreAdapterMvc(this IMvcBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddApplicationPart(typeof(MvcConfigurationExtensions).Assembly);
#if NETSTANDARD2_0
            builder.AddJsonOptions(options => options.SerializerSettings.AddDataCoreAdapterConverters());
#else
            builder.AddJsonOptions(options => options.JsonSerializerOptions.AddDataCoreAdapterConverters());
#endif

            return builder;
        }

    }

}
