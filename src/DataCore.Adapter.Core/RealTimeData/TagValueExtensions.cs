﻿using System;
using System.Collections.Generic;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extension methods for <see cref="TagValue"/>.
    /// </summary>
    public static class TagValueExtensions {

        /// <summary>
        /// Casts the <see cref="TagValue.Value"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to cast the <see cref="TagValue.Value"/> to.
        /// </typeparam>
        /// <param name="value">
        ///   The <see cref="TagValue"/>.
        /// </param>
        /// <returns>
        ///   The value of the <see cref="TagValue"/> cast to <typeparamref name="T"/>, or the 
        ///   default value of <typeparamref name="T"/> if the cast was unsuccessful.
        /// </returns>
        public static T GetValueOrDefault<T>(this TagValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            return value.GetValueOrDefault<T>(default!);
        }


        /// <summary>
        /// Casts the <see cref="TagValue.Value"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to cast the <see cref="TagValue.Value"/> to.
        /// </typeparam>
        /// <param name="value">
        ///   The <see cref="TagValue"/>.
        /// </param>
        /// <param name="defaultValue">
        ///   The default value to return if the cast was unsuccessful.
        /// </param>
        /// <returns>
        ///   The value of the <see cref="TagValue"/> cast to <typeparamref name="T"/>, or the 
        ///   <paramref name="defaultValue"/> if the cast was unsuccessful.
        /// </returns>
        public static T GetValueOrDefault<T>(this TagValue value, T defaultValue) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            return value.Value.GetValueOrDefault<T>(defaultValue);
        }


        /// <summary>
        /// Gets a collection containing both the <see cref="TagValue.Value"/> and <see cref="TagValue.AdditionalValues"/> 
        /// defined on the <see cref="TagValue"/>.
        /// </summary>
        /// <param name="value">
        ///   The <see cref="TagValue"/>.
        /// </param>
        /// <returns>
        ///   An enumerable of <see cref="Variant"/> instances representing all of the values of 
        ///   the <see cref="TagValue"/>.
        /// </returns>
        public static IEnumerable<Variant> GetAllValues(this TagValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            yield return value.Value;

            foreach (var item in value.AdditionalValues) {
                yield return item;
            }
        }

    }
}
