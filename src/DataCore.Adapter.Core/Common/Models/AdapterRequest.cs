﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// Base class that adapter request objects can inherit from.
    /// </summary>
    public abstract class AdapterRequest : IValidatableObject {

        /// <summary>
        /// Additional request properties. These can be used to provide bespoke query parameters 
        /// supported by the adapter.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }


        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext) {
            return Validate(validationContext);
        }


        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        protected virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            return new ValidationResult[0];
        }
    }
}