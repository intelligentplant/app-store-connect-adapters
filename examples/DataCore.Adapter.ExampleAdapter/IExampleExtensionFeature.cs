﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    public interface IExampleExtensionFeature : IAdapterExtensionFeature {

        /// <summary>
        /// Gets the current time.
        /// </summary>
        /// <returns>
        ///   The current time.
        /// </returns>
        DateTime GetCurrentTime();

    }
}