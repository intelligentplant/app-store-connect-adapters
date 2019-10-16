﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a custom property associated with an adapter, tag, tag value, event message, etc.
    /// </summary>
    public class AdapterProperty {

        /// <summary>
        /// The property name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The property value.
        /// </summary>
        public Variant Value { get; set; }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <param name="valueType">
        ///   The value type of the property.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty Create(string name, object value, VariantType valueType) {
            return new AdapterProperty() { 
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Value = new Variant() {
                    Value = value,
                    Type = valueType
                }
            };
        }


        /// <summary>
        /// Creates a new <see cref="AdapterProperty"/> object.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The value of the property.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty Create(string name, object value) {
            return new AdapterProperty() {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Value = Variant.Create(value)
            };
        }


        /// <summary>
        /// Creates a copy of an existing <see cref="AdapterProperty"/>.
        /// </summary>
        /// <param name="property">
        ///   The property to clone.
        /// </param>
        /// <returns>
        ///   A copy of the property.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterProperty FromExisting(AdapterProperty property) {
            if (property == null) {
                throw new ArgumentNullException(nameof(property));
            }

            return Create(property.Name, property.Value);
        }

    }
}