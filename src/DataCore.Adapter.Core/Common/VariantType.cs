﻿namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the type of a variant value.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Enum members all refer to data types")]
    public enum VariantType {

        /// <summary>
        /// Unknown value type.
        /// </summary>
        Unknown,

        /// <summary>
        /// No value.
        /// </summary>
        Null,

        /// <summary>
        /// Custom object.
        /// </summary>
        Object,

        /// <summary>
        /// Boolean.
        /// </summary>
        Boolean,

        /// <summary>
        /// Signed byte.
        /// </summary>
        SByte,

        /// <summary>
        /// Unsigned byte.
        /// </summary>
        Byte,

        /// <summary>
        /// Signed 16-bit integer.
        /// </summary>
        Int16,

        /// <summary>
        /// Unsigned 16-bit integer.
        /// </summary>
        UInt16,

        /// <summary>
        /// Signed 32-bit integer.
        /// </summary>
        Int32,

        /// <summary>
        /// Unsigned 32-bit integer.
        /// </summary>
        UInt32,

        /// <summary>
        /// Signed 64-bit integer.
        /// </summary>
        Int64,

        /// <summary>
        /// Unsigned 64-bit integer.
        /// </summary>
        UInt64,

        /// <summary>
        /// Single precision floating point number.
        /// </summary>
        Float,

        /// <summary>
        /// Double precision floating point number.
        /// </summary>
        Double,

        /// <summary>
        /// String.
        /// </summary>
        String,

        /// <summary>
        /// Timestamp.
        /// </summary>
        DateTime,

        /// <summary>
        /// Time span.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// URL
        /// </summary>
        Url

    }

}
